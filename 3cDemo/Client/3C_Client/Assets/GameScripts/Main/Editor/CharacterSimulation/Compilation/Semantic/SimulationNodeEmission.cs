using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public readonly struct SimulationNodeEmission
    {
        public SimulationNodeEmission(
            SimulationOperationCode code,
            int integer0 = 0,
            int integer1 = 0,
            ulong unsigned0 = 0,
            string text0 = null,
            uint flags = 0,
            IEnumerable<KeyValuePair<string, object>> constants = null)
        {
            Code = code;
            Integer0 = integer0;
            Integer1 = integer1;
            Unsigned0 = unsigned0;
            Text0 = text0 ?? string.Empty;
            Flags = flags;
            Constants = constants == null
                ? Array.Empty<KeyValuePair<string, object>>()
                : constants.ToArray();
        }

        public SimulationOperationCode Code { get; }
        public int Integer0 { get; }
        public int Integer1 { get; }
        public ulong Unsigned0 { get; }
        public string Text0 { get; }
        public uint Flags { get; }
        public IReadOnlyList<KeyValuePair<string, object>> Constants { get; }
    }
}
