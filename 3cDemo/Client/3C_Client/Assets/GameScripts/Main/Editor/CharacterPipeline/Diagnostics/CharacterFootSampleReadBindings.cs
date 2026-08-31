using System.Collections.Generic;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterFootSampleReadBindings
    {
        internal CharacterFootSampleReadBindings(Dictionary<string, int> indices)
        {
            Resolved = CharacterFootResolvedColumns.Schema.Bind(indices);
            CurrentSupport = CharacterFootCurrentSupportColumns.Schema.Bind(indices);
            SelectedTarget = CharacterFootSupportTargetColumns.Selected.Bind(indices);
            CurrentStep = CharacterFootStepColumns.Current.Bind(indices);
            IncomingStep = CharacterFootStepColumns.Incoming.Bind(indices);
            SelectedPhase = CharacterFootStepColumns.SelectedPhase.Bind(indices);
            OutputEvents = CharacterFootEventColumns.Output.Bind(indices);
            InputEvents = CharacterFootEventColumns.Input.Bind(indices);
            Pelvis = CharacterFootPelvisColumns.Schema.Bind(indices);
            Solver = CharacterFootSolverColumns.Schema.Bind(indices);
        }

        internal CharacterFootCsvReader<CharacterFootResolvedSample> Resolved { get; }
        internal CharacterFootCsvReader<CharacterFootCurrentSupportSample> CurrentSupport { get; }
        internal CharacterFootCsvReader<CharacterFootSupportTargetSample> SelectedTarget { get; }
        internal CharacterFootCsvReader<CharacterFootStepCandidateSample> CurrentStep { get; }
        internal CharacterFootCsvReader<CharacterFootStepCandidateSample> IncomingStep { get; }
        internal CharacterFootCsvReader<CharacterFootStepPhaseSample> SelectedPhase { get; }
        internal CharacterFootCsvReader<CharacterFootEventSample> OutputEvents { get; }
        internal CharacterFootCsvReader<CharacterFootEventSample> InputEvents { get; }
        internal CharacterFootCsvReader<CharacterFootPelvisSample> Pelvis { get; }
        internal CharacterFootCsvReader<CharacterFootSolverSample> Solver { get; }
    }
}
