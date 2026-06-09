using NUnit.Framework;
using ThirdPersonInput;

namespace ThirdPersonInput.Tests
{
    public sealed class InputRequestBufferTests
    {
        static readonly InputBufferSettings Settings = new InputBufferSettings(6, 3, 4, 2);

        [Test]
        public void ButtonStatePressedWhenHeldStarts()
        {
            InputButtonState state = InputButtonState.FromHeld(false, true);

            Assert.True(state.Pressed);
            Assert.True(state.Held);
            Assert.False(state.Released);
        }

        [Test]
        public void ButtonStateHeldDoesNotRepeatPressed()
        {
            InputButtonState state = InputButtonState.FromHeld(true, true);

            Assert.False(state.Pressed);
            Assert.True(state.Held);
            Assert.False(state.Released);
        }

        [Test]
        public void ButtonStateReleasedWhenHeldEnds()
        {
            InputButtonState state = InputButtonState.FromHeld(true, false);

            Assert.False(state.Pressed);
            Assert.False(state.Held);
            Assert.True(state.Released);
        }

        [Test]
        public void ButtonStateInactiveHasNoEdges()
        {
            InputButtonState state = InputButtonState.FromHeld(false, false);

            Assert.False(state.Pressed);
            Assert.False(state.Held);
            Assert.False(state.Released);
        }

        [Test]
        public void PressedButtonCreatesRequest()
        {
            InputRequestBuffer buffer = new InputRequestBuffer();

            buffer.AddFromButtonState(InputButtonKind.Attack, InputButtonState.FromHeld(false, true), 10, Settings);

            Assert.True(buffer.TryPeek(InputRequestKind.Attack, 10, out BufferedInputRequest request));
            Assert.AreEqual(InputRequestKind.Attack, request.Kind);
            Assert.AreEqual(InputButtonKind.Attack, request.SourceButton);
            Assert.AreEqual(10, request.OriginStep);
            Assert.AreEqual(16, request.ExpireStep);
        }

        [Test]
        public void DifferentRequestKindsUseDifferentWindows()
        {
            InputRequestBuffer buffer = new InputRequestBuffer();

            buffer.AddFromButtonState(InputButtonKind.Attack, InputButtonState.FromHeld(false, true), 4, Settings);
            buffer.AddFromButtonState(InputButtonKind.Dodge, InputButtonState.FromHeld(false, true), 4, Settings);

            Assert.True(buffer.TryPeek(InputRequestKind.Attack, 4, out BufferedInputRequest attack));
            Assert.True(buffer.TryPeek(InputRequestKind.Dodge, 4, out BufferedInputRequest dodge));
            Assert.AreEqual(10, attack.ExpireStep);
            Assert.AreEqual(7, dodge.ExpireStep);
        }

        [Test]
        public void ZeroWindowRequestOnlyAvailableAtOriginStep()
        {
            InputRequestBuffer buffer = new InputRequestBuffer();
            InputBufferSettings settings = new InputBufferSettings(0, 3, 4, 2);

            buffer.AddFromButtonState(InputButtonKind.Attack, InputButtonState.FromHeld(false, true), 8, settings);

            Assert.True(buffer.TryPeek(InputRequestKind.Attack, 8, out _));
            Assert.False(buffer.TryPeek(InputRequestKind.Attack, 9, out _));
        }

        [Test]
        public void RequestIsAvailableInsideWindow()
        {
            InputRequestBuffer buffer = new InputRequestBuffer();

            buffer.AddFromButtonState(InputButtonKind.Attack, InputButtonState.FromHeld(false, true), 10, Settings);

            Assert.True(buffer.TryPeek(InputRequestKind.Attack, 15, out _));
        }

        [Test]
        public void RequestIsUnavailableAfterExpireStep()
        {
            InputRequestBuffer buffer = new InputRequestBuffer();

            buffer.AddFromButtonState(InputButtonKind.Attack, InputButtonState.FromHeld(false, true), 10, Settings);

            Assert.False(buffer.TryPeek(InputRequestKind.Attack, 17, out _));
        }

        [Test]
        public void ConsumedRequestCannotBeConsumedAgain()
        {
            InputRequestBuffer buffer = new InputRequestBuffer();

            buffer.AddFromButtonState(InputButtonKind.Attack, InputButtonState.FromHeld(false, true), 10, Settings);

            Assert.True(buffer.TryConsume(InputRequestKind.Attack, 12, out BufferedInputRequest consumed));
            Assert.True(consumed.Consumed);
            Assert.False(buffer.TryConsume(InputRequestKind.Attack, 12, out _));
        }

        [Test]
        public void ClearRemovesRequests()
        {
            InputRequestBuffer buffer = new InputRequestBuffer();

            buffer.AddFromButtonState(InputButtonKind.Attack, InputButtonState.FromHeld(false, true), 10, Settings);
            buffer.Clear();

            Assert.AreEqual(0, buffer.Count);
            Assert.False(buffer.TryPeek(InputRequestKind.Attack, 10, out _));
        }

        [Test]
        public void SameButtonSequenceBuildsSameRequestResults()
        {
            InputRequestBuffer first = BuildAttackBufferFromHeldSequence(false, true, true, false);
            InputRequestBuffer second = BuildAttackBufferFromHeldSequence(false, true, true, false);

            Assert.AreEqual(first.Count, second.Count);
            Assert.AreEqual(first.Requests[0].Kind, second.Requests[0].Kind);
            Assert.AreEqual(first.Requests[0].OriginStep, second.Requests[0].OriginStep);
            Assert.AreEqual(first.Requests[0].ExpireStep, second.Requests[0].ExpireStep);
            Assert.AreEqual(first.Requests[0].Consumed, second.Requests[0].Consumed);
        }

        [Test]
        public void NegativeWindowIsClampedToOriginStep()
        {
            InputRequestBuffer buffer = new InputRequestBuffer();
            InputBufferSettings settings = new InputBufferSettings(-4, 3, 4, 2);

            buffer.AddFromButtonState(InputButtonKind.Attack, InputButtonState.FromHeld(false, true), 5, settings);

            Assert.True(buffer.TryPeek(InputRequestKind.Attack, 5, out BufferedInputRequest request));
            Assert.AreEqual(5, request.ExpireStep);
            Assert.False(buffer.TryPeek(InputRequestKind.Attack, 6, out _));
        }

        [Test]
        public void BufferDoesNotNeedUnitySceneObjects()
        {
            InputRequestBuffer buffer = new InputRequestBuffer();

            buffer.AddFromButtonState(InputButtonKind.Interact, InputButtonState.FromHeld(false, true), 2, Settings);

            Assert.True(buffer.TryConsume(InputRequestKind.Interact, 3, out BufferedInputRequest request));
            Assert.AreEqual(InputRequestKind.Interact, request.Kind);
        }

        static InputRequestBuffer BuildAttackBufferFromHeldSequence(params bool[] heldSequence)
        {
            InputRequestBuffer buffer = new InputRequestBuffer();
            bool previousHeld = false;

            for (int step = 0; step < heldSequence.Length; step++)
            {
                InputButtonState state = InputButtonState.FromHeld(previousHeld, heldSequence[step]);
                buffer.AddFromButtonState(InputButtonKind.Attack, state, step, Settings);
                previousHeld = heldSequence[step];
            }

            return buffer;
        }
    }
}
