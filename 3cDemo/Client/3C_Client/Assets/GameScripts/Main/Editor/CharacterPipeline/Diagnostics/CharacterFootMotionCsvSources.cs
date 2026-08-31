using UnityEngine;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal readonly struct CharacterFootMotionCoreCsvSource
    {
        internal CharacterFootMotionCoreCsvSource(
            in CharacterFootLandingPredictionFootDiagnostics foot,
            in CharacterFootSwingMotionDiagnostics motion,
            float baselineSampleAlongUp, float envelopeSampleAlongUp,
            float formalFootHeight, float rawFormalTargetHeight,
            float envelopeMinimumCorrection, float builderSelectedCorrection,
            bool builderSwingTargetAvailable, Vector3 builderSwingTargetCorrection,
            CharacterFootSwingPathHorizontalAxisState horizontalAxisState,
            in CharacterFootActualEnvelopeIntersectionFact actualEnvelope,
            bool actualEnvelopeCorrectionAvailable,
            float actualEnvelopeMinimumCorrection,
            float actualEnvelopeAdvanceAboveBuilderTarget,
            CharacterFootContactPlanePenetrationAvailability penetrationAvailability)
        {
            Foot = foot;
            Motion = motion;
            BaselineSampleAlongUp = baselineSampleAlongUp;
            EnvelopeSampleAlongUp = envelopeSampleAlongUp;
            FormalFootHeight = formalFootHeight;
            RawFormalTargetHeight = rawFormalTargetHeight;
            EnvelopeMinimumCorrection = envelopeMinimumCorrection;
            BuilderSelectedCorrection = builderSelectedCorrection;
            BuilderSwingTargetAvailable = builderSwingTargetAvailable;
            BuilderSwingTargetCorrection = builderSwingTargetCorrection;
            HorizontalAxisState = horizontalAxisState;
            ActualEnvelope = actualEnvelope;
            ActualEnvelopeCorrectionAvailable = actualEnvelopeCorrectionAvailable;
            ActualEnvelopeMinimumCorrection = actualEnvelopeMinimumCorrection;
            ActualEnvelopeAdvanceAboveBuilderTarget = actualEnvelopeAdvanceAboveBuilderTarget;
            PenetrationAvailability = penetrationAvailability;
        }
        internal CharacterFootLandingPredictionFootDiagnostics Foot { get; }
        internal CharacterFootSwingMotionDiagnostics Motion { get; }
        internal float BaselineSampleAlongUp { get; }
        internal float EnvelopeSampleAlongUp { get; }
        internal float FormalFootHeight { get; }
        internal float RawFormalTargetHeight { get; }
        internal float EnvelopeMinimumCorrection { get; }
        internal float BuilderSelectedCorrection { get; }
        internal bool BuilderSwingTargetAvailable { get; }
        internal Vector3 BuilderSwingTargetCorrection { get; }
        internal CharacterFootSwingPathHorizontalAxisState HorizontalAxisState { get; }
        internal CharacterFootActualEnvelopeIntersectionFact ActualEnvelope { get; }
        internal bool ActualEnvelopeCorrectionAvailable { get; }
        internal float ActualEnvelopeMinimumCorrection { get; }
        internal float ActualEnvelopeAdvanceAboveBuilderTarget { get; }
        internal CharacterFootContactPlanePenetrationAvailability PenetrationAvailability { get; }
    }

    internal readonly struct CharacterFootGoalCsvSource
    {
        internal CharacterFootGoalCsvSource(
            in CharacterFullBodyIkGoal goal, Vector3 originalAnkle,
            CharacterFullBodyIkGoal pelvisGoal)
        {
            Goal = goal;
            OriginalAnkle = originalAnkle;
            PelvisGoal = pelvisGoal;
        }
        internal CharacterFullBodyIkGoal Goal { get; }
        internal Vector3 OriginalAnkle { get; }
        internal CharacterFullBodyIkGoal PelvisGoal { get; }
    }
}
