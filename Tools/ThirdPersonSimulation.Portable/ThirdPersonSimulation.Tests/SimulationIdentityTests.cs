using System;
using System.Collections.Generic;
using NUnit.Framework;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Tests
{
    [TestFixture]
    public sealed class SimulationIdentityTests
    {
        [Test]
        public void StableHashHasCanonicalFormatAndStableValue()
        {
            StableHash first = StableHash.Compute("program", "actor", "tick");
            StableHash second = StableHash.Compute("program", "actor", "tick");
            StableHash changed = StableHash.Compute("program", "actor", "next-tick");

            Assert.That(first.Value, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(second, Is.EqualTo(first));
            Assert.That(changed, Is.Not.EqualTo(first));
            Assert.Throws<ArgumentException>(new Action(() => new StableHash(new string('A', 64))));
        }

        [Test]
        public void EventIdIsStableAndChangesWithEveryIdentityInput()
        {
            var program = new ProgramHash(StableHash.Compute("program"));
            var actor = new ActorId("actor");
            var activation = new ActivationId(new OperationHandle(4), 2, "root/action");
            var tick = new SimulationTick(8);
            EventId baseline = EventId.Create(program, actor, activation, tick, 3, "presentation");

            EventId repeated = EventId.Create(program, actor, activation, tick, 3, "presentation");
            var changedIds = new List<EventId>
            {
                EventId.Create(new ProgramHash(StableHash.Compute("other-program")), actor, activation, tick, 3, "presentation"),
                EventId.Create(program, new ActorId("other-actor"), activation, tick, 3, "presentation"),
                EventId.Create(program, actor, new ActivationId(new OperationHandle(4), 3, "root/action"), tick, 3, "presentation"),
                EventId.Create(program, actor, activation, new SimulationTick(9), 3, "presentation"),
                EventId.Create(program, actor, activation, tick, 4, "presentation"),
                EventId.Create(program, actor, activation, tick, 3, "fact")
            };

            Assert.That(repeated, Is.EqualTo(baseline));
            for (int i = 0; i < changedIds.Count; i++)
                Assert.That(changedIds[i], Is.Not.EqualTo(baseline));
        }
    }
}
