using UnityEngine;
using ThirdPersonCharacter.Pipeline.Presentation;
using Source = ThirdPersonCharacter.Pipeline.Editor.CharacterFootSampleCsvSource;
using Record = ThirdPersonCharacter.Pipeline.Editor.CharacterFootMotionDiagnosticAnalyzer.FootFrame;
using Column = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvColumn<ThirdPersonCharacter.Pipeline.Editor.CharacterFootSampleCsvSource, ThirdPersonCharacter.Pipeline.Editor.CharacterFootMotionDiagnosticAnalyzer.FootFrame>;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal readonly struct CharacterFootSampleCsvSource
    {
        internal CharacterFootSampleCsvSource(
            in CharacterFootIdentityCsvSource identity,
            in CharacterFootStepCandidateDiagnostics selectedPhase,
            in CharacterFootStepCandidateDiagnostics currentStep,
            in CharacterFootStepCandidateDiagnostics incomingStep,
            in CharacterFootFormalObservationCsvSource formalOutput,
            in CharacterFootEventCsvSource outputEvents,
            in CharacterFootFormalInputCsvSource formalInput,
            in CharacterFootEventCsvSource inputEvents,
            Vector3 rootLanding,
            in CharacterFootLandingPredictionInputDiagnostics input,
            in CharacterFootPrimarySupportDiagnostics primarySupport,
            in CharacterFootLandingPredictionSampler.RootHierarchyCapture rootHierarchy,
            in CharacterFootLandingObservationCsvSource landingObservation,
            in CharacterFootGroundPathDiagnostics groundPath,
            in CharacterFootMotionCoreCsvSource motionCore,
            in CharacterFootSwingMotionDiagnostics motion,
            in CharacterFootSupportTargetDiagnostics selectedTarget,
            in CharacterFootCurrentSupportDiagnostics currentSupport,
            in CharacterResolvedFootDiagnostics resolved,
            in CharacterFootGoalCsvSource goal,
            in CharacterFootPelvisCsvSource pelvis,
            in CharacterFootSolverCsvSource solver)
        {
            Identity = identity;
            SelectedPhase = selectedPhase;
            CurrentStep = currentStep;
            IncomingStep = incomingStep;
            FormalOutput = formalOutput;
            OutputEvents = outputEvents;
            FormalInput = formalInput;
            InputEvents = inputEvents;
            RootLanding = rootLanding;
            Input = input;
            PrimarySupport = primarySupport;
            RootHierarchy = rootHierarchy;
            LandingObservation = landingObservation;
            GroundPath = groundPath;
            MotionCore = motionCore;
            Motion = motion;
            SelectedTarget = selectedTarget;
            CurrentSupport = currentSupport;
            Resolved = resolved;
            Goal = goal;
            Pelvis = pelvis;
            Solver = solver;
        }

        internal CharacterFootIdentityCsvSource Identity { get; }
        internal CharacterFootStepCandidateDiagnostics SelectedPhase { get; }
        internal CharacterFootStepCandidateDiagnostics CurrentStep { get; }
        internal CharacterFootStepCandidateDiagnostics IncomingStep { get; }
        internal CharacterFootFormalObservationCsvSource FormalOutput { get; }
        internal CharacterFootEventCsvSource OutputEvents { get; }
        internal CharacterFootFormalInputCsvSource FormalInput { get; }
        internal CharacterFootEventCsvSource InputEvents { get; }
        internal Vector3 RootLanding { get; }
        internal CharacterFootLandingPredictionInputDiagnostics Input { get; }
        internal CharacterFootPrimarySupportDiagnostics PrimarySupport { get; }
        internal CharacterFootLandingPredictionSampler.RootHierarchyCapture RootHierarchy { get; }
        internal CharacterFootLandingObservationCsvSource LandingObservation { get; }
        internal CharacterFootGroundPathDiagnostics GroundPath { get; }
        internal CharacterFootMotionCoreCsvSource MotionCore { get; }
        internal CharacterFootSwingMotionDiagnostics Motion { get; }
        internal CharacterFootSupportTargetDiagnostics SelectedTarget { get; }
        internal CharacterFootCurrentSupportDiagnostics CurrentSupport { get; }
        internal CharacterResolvedFootDiagnostics Resolved { get; }
        internal CharacterFootGoalCsvSource Goal { get; }
        internal CharacterFootPelvisCsvSource Pelvis { get; }
        internal CharacterFootSolverCsvSource Solver { get; }
    }

    internal static class CharacterFootSampleColumns
    {
        internal static readonly CharacterFootCsvGroup<Source, Record> Schema =
            new CharacterFootCsvGroup<Source, Record>(
                "Sample", () => new Record(), new Column[]
                {
                    Column.Compose(CharacterFootIdentityColumns.Schema, (in Source source) => source.Identity, (target, value) => target.Identity = value),
                    Column.Compose(CharacterFootStepColumns.SelectedPhase, (in Source source) => source.SelectedPhase, (target, value) => target.SelectedPhase = value),
                    Column.Compose(CharacterFootStepColumns.Current, (in Source source) => source.CurrentStep, (target, value) => target.CurrentStep = value),
                    Column.Compose(CharacterFootStepColumns.Incoming, (in Source source) => source.IncomingStep, (target, value) => target.IncomingStep = value),
                    Column.Compose(CharacterFootFormalObservationColumns.Output, (in Source source) => source.FormalOutput, (target, value) => target.FormalOutput = value),
                    Column.Compose(CharacterFootEventColumns.Output, (in Source source) => source.OutputEvents, (target, value) => target.OutputEvents = value),
                    Column.Compose(CharacterFootFormalObservationColumns.Input, (in Source source) => source.FormalInput, (target, value) => target.FormalInput = value),
                    Column.Compose(CharacterFootEventColumns.Input, (in Source source) => source.InputEvents, (target, value) => target.InputEvents = value),
                    Column.Compose(CharacterFootRootLandingColumns.Schema, (in Source source) => source.RootLanding, (target, value) => target.RootLanding = value),
                    Column.Compose(CharacterFootTimingColumns.Schema, (in Source source) => source.Input, (target, value) => target.Timing = value),
                    Column.Compose(CharacterFootPredictionMotionColumns.Schema, (in Source source) => source.Input, (target, value) => target.PredictionMotion = value),
                    Column.Compose(CharacterFootActionColumns.Schema, (in Source source) => source.Input, (target, value) => target.Action = value),
                    Column.Compose(CharacterFootPrimarySupportColumns.Schema, (in Source source) => source.PrimarySupport, (target, value) => target.PrimarySupport = value),
                    Column.Compose(CharacterFootRootHierarchyColumns.Schema, (in Source source) => source.RootHierarchy, (target, value) => target.RootHierarchy = value),
                    Column.Compose(CharacterFootBodyCorrectionColumns.Schema, (in Source source) => source.Input, (target, value) => target.BodyCorrection = value),
                    Column.Compose(CharacterFootLandingObservationColumns.Schema, (in Source source) => source.LandingObservation, (target, value) => target.LandingObservation = value),
                    Column.Compose(CharacterFootGroundPathColumns.Schema, (in Source source) => source.GroundPath, (target, value) => target.GroundPath = value),
                    Column.Compose(CharacterFootMotionCoreColumns.Schema, (in Source source) => source.MotionCore, (target, value) => target.MotionCore = value),
                    Column.Compose(CharacterFootPathContinuityColumns.Schema, (in Source source) => source.Motion.PathContinuity, (target, value) => target.PathContinuity = value),
                    Column.Compose(CharacterFootLifecycleColumns.Schema, (in Source source) => source.Motion.Lifecycle, (target, value) => target.Lifecycle = value),
                    Column.Compose(CharacterFootOutputStagesColumns.Schema, (in Source source) => source.Motion.OutputStages, (target, value) => target.OutputStages = value),
                    Column.Compose(CharacterFootSupportTargetColumns.Selected, (in Source source) => source.SelectedTarget, (target, value) => target.SelectedSupportTarget = value),
                    Column.Compose(CharacterFootResponseColumns.Schema, (in Source source) => source.Motion.Response, (target, value) => target.Response = value),
                    Column.Compose(CharacterFootCurrentSupportColumns.Schema, (in Source source) => source.CurrentSupport, (target, value) => target.CurrentSupport = value),
                    Column.Compose(CharacterFootResolvedColumns.Schema, (in Source source) => source.Resolved, (target, value) => target.Resolved = value),
                    Column.Compose(CharacterFootGoalColumns.Schema, (in Source source) => source.Goal, (target, value) => target.Goal = value),
                    Column.Compose(CharacterFootPelvisColumns.Schema, (in Source source) => source.Pelvis, (target, value) => target.Pelvis = value),
                    Column.Compose(CharacterFootSolverColumns.Schema, (in Source source) => source.Solver, (target, value) => target.Solver = value)
                });
    }
}
