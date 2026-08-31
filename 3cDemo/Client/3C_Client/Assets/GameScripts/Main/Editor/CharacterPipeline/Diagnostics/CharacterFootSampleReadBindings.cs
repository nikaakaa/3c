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
            PathContinuity = CharacterFootPathContinuityColumns.Schema.Bind(indices);
            Lifecycle = CharacterFootLifecycleColumns.Schema.Bind(indices);
            OutputStages = CharacterFootOutputStagesColumns.Schema.Bind(indices);
            Timing = CharacterFootTimingColumns.Schema.Bind(indices);
            PredictionMotion = CharacterFootPredictionMotionColumns.Schema.Bind(indices);
            PrimarySupport = CharacterFootPrimarySupportColumns.Schema.Bind(indices);
            RootHierarchy = CharacterFootRootHierarchyColumns.Schema.Bind(indices);
            BodyCorrection = CharacterFootBodyCorrectionColumns.Schema.Bind(indices);
            LandingObservation = CharacterFootLandingObservationColumns.Schema.Bind(indices);
            FormalOutput = CharacterFootFormalObservationColumns.Output.Bind(indices);
            FormalInput = CharacterFootFormalObservationColumns.Input.Bind(indices);
            GroundPath = CharacterFootGroundPathColumns.Schema.Bind(indices);
            Response = CharacterFootResponseColumns.Schema.Bind(indices);
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
        internal CharacterFootCsvReader<CharacterFootResponseSample> Response { get; }
        internal CharacterFootCsvReader<CharacterFootGroundPathSample> GroundPath { get; }
        internal CharacterFootCsvReader<CharacterFootLandingObservationSample> LandingObservation { get; }
        internal CharacterFootCsvReader<CharacterFootFormalObservationSample> FormalOutput { get; }
        internal CharacterFootCsvReader<CharacterFootFormalInputSample> FormalInput { get; }
        internal CharacterFootCsvReader<CharacterFootBodyCorrectionSample> BodyCorrection { get; }
        internal CharacterFootCsvReader<CharacterFootRootHierarchySample> RootHierarchy { get; }
        internal CharacterFootCsvReader<CharacterFootPrimarySupportSample> PrimarySupport { get; }
        internal CharacterFootCsvReader<CharacterFootPredictionMotionSample> PredictionMotion { get; }
        internal CharacterFootCsvReader<CharacterFootTimingSample> Timing { get; }
        internal CharacterFootCsvReader<CharacterFootOutputStagesSample> OutputStages { get; }
        internal CharacterFootCsvReader<CharacterFootLifecycleSample> Lifecycle { get; }
        internal CharacterFootCsvReader<CharacterFootPathContinuitySample> PathContinuity { get; }
    }
}
