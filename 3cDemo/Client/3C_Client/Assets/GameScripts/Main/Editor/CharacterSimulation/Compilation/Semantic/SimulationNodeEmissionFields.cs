using System.Collections.Generic;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public static class SimulationNodeEmissionFields
    {
        public static KeyValuePair<string, object>[] Fields(params (string Name, object Value)[] values)
        {
            KeyValuePair<string, object>[] result = new KeyValuePair<string, object>[values.Length];
            for (int i = 0; i < values.Length; i++)
                result[i] = new KeyValuePair<string, object>(values[i].Name, values[i].Value);
            return result;
        }
    }
}
