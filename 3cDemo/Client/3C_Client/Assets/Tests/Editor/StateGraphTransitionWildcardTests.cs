using System.IO;
using NUnit.Framework;
using ThirdPersonCharacterStateMachine;

namespace Tests.Editor
{
    public sealed class StateGraphTransitionWildcardTests
    {
        [Test]
        public void DotWildcardMatchesDomainStateIds()
        {
            StateGraphTransition transition = new StateGraphTransition(
                "Locomotion.*",
                new StateGraphNodeId("Action.Dodge"),
                0);

            Assert.True(transition.MatchesSource(new StateGraphNodeId("Locomotion.Idle")));
            Assert.True(transition.MatchesSource(new StateGraphNodeId("Locomotion.MoveLoop")));
            Assert.False(transition.MatchesSource(new StateGraphNodeId("Action.Dodge")));
        }

        [Test]
        public void CorinLocomotionStateGraphContainsOnlyLocomotionDomainNodes()
        {
            string asset = File.ReadAllText("Assets/Configs/3C/StateMachine/Locomotion/Corin/CorinLocomotionStateGraph.asset");

            Assert.That(asset, Does.Not.Contain("stateId: FullBody"));
            Assert.That(asset, Does.Not.Contain("parentStateId: FullBody"));
            Assert.That(asset, Does.Not.Contain("parentStateId: Locomotion"));
            Assert.That(asset, Does.Not.Contain("parentStateId: Action"));
            Assert.That(asset, Does.Contain("stateId: Locomotion.Idle"));
            Assert.That(asset, Does.Not.Contain("stateId: Action."));
            Assert.That(asset, Does.Not.Contain("fromStateId: Action."));
            Assert.That(asset, Does.Not.Contain("toStateId: Action."));
        }
    }
}
