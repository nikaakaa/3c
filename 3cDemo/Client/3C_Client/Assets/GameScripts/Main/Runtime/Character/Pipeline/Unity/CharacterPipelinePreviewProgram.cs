using System;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline
{
    internal static class CharacterPipelinePreviewProgram
    {
        public static OperationHandle FindTimelineOperation(
            CharacterSimulationProgram program,
            string timelineAuthoringId)
        {
            OperationHandle result = OperationHandle.Invalid;
            for (int i = 0; i < program.Operations.Count; i++)
            {
                SimulationOperation operation = program.Operations[i];
                if (operation.Code != SimulationOperationCode.Timeline ||
                    !string.Equals(operation.Text0, timelineAuthoringId, StringComparison.Ordinal))
                    continue;
                if (!result.IsValid || operation.Handle.CompareTo(result) < 0)
                    result = operation.Handle;
            }
            if (!result.IsValid)
                throw new InvalidOperationException(
                    $"Timeline '{timelineAuthoringId}' is absent from the compiled Program.");
            return result;
        }
    }
}
