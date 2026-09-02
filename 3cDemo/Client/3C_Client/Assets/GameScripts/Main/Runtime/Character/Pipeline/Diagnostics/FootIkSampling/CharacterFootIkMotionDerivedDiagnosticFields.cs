using System;
using ThirdPerson.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
    internal enum CharacterFootIkSwingPathHorizontalAxisState
    {
        Unavailable = 0,
        Available = 1,
        InvalidComponentUp = 2,
        DegenerateAxis = 3
    }

    internal enum CharacterFootIkActualEnvelopeIntersectionState
    {
        Unavailable = 0,
        InvalidComponentUp = 1,
        DegenerateAxis = 2,
        NoIntersection = 3,
        Unique = 4,
        AmbiguousEnvelopeAtActualFootDistance = 5
    }

    internal enum CharacterFootIkActualFootAxisRegion
    {
        Unavailable = 0,
        BeforePathStart = 1,
        WithinPathSegment = 2,
        AfterPathEnd = 3
    }

    internal enum CharacterFootIkActualEnvelopeCounterfactualState
    {
        Unavailable = 0,
        UniqueInCorridor = 1,
        AmbiguousInCorridor = 2,
        OutsideGroundPathCorridor = 3,
        NoIntersection = 4
    }

    internal enum CharacterFootIkContactPlanePenetrationAvailability : byte
    {
        Available = 0,
        FinalPhysicalPoseUnavailable = 1,
        ContactLifecycleUnavailable = 2,
        ContactPlaneUnavailable = 3,
        EventLineageMismatch = 4,
        SurfaceLineageMismatch = 5,
        InvalidContactNormal = 6
    }

    internal struct CharacterFootIkActualEnvelopeFacts
    {
        internal CharacterFootIkActualEnvelopeIntersectionState State;
        internal float ActualFootHorizontalDistance;
        internal float BaselineHorizontalDistance;
        internal float EnvelopeHorizontalDistance;
        internal CharacterFootIkActualFootAxisRegion AxisRegion;
        internal float ClosestPathParameter;
        internal float DistanceAlongAxis;
        internal float CrossTrackDistance;
        internal float CorridorRadius;
        internal bool WithinGroundPathCorridor;
        internal int CandidateCount;
        internal float MinimumHeightAlongUp;
        internal float MaximumHeightAlongUp;
        internal float HeightSpan;
        internal bool HasVerticalEdge;
        internal bool HasMultipleHeights;
        internal bool Ambiguous;
        internal CharacterFootIkActualEnvelopeCounterfactualState CounterfactualState;
    }

    internal struct CharacterFootIkMotionCoreDerivedFacts
    {
        const float HorizontalEpsilonMeters = 0.001f;
        const float HeightEpsilonMeters = 0.001f;
        const int MaximumEnvelopeHeightCount = 136;

        internal float BaselineSampleAlongUp;
        internal float EnvelopeSampleAlongUp;
        internal float FormalFootHeight;
        internal float RawFormalTargetHeight;
        internal float EnvelopeMinimumCorrection;
        internal float BuilderSelectedCorrection;
        internal bool BuilderSwingTargetAvailable;
        internal Vector3 BuilderSwingTargetCorrection;
        internal CharacterFootIkSwingPathHorizontalAxisState HorizontalAxisState;
        internal CharacterFootIkActualEnvelopeFacts ActualEnvelope;
        internal bool ActualEnvelopeCorrectionAvailable;
        internal float ActualEnvelopeMinimumCorrection;
        internal float ActualEnvelopeAdvanceAboveBuilderTarget;
        internal CharacterFootIkContactPlanePenetrationAvailability PenetrationAvailability;

        internal static CharacterFootIkMotionCoreDerivedFacts Resolve(
            in CharacterFootIkCommittedCaptureViewLease view,
            CharacterFootSide side)
        {
            ref readonly CharacterFootLandingPredictionDiagnostics frame =
                ref view.LandingPrediction;
            CharacterFootLandingPredictionFootDiagnostics foot =
                side == CharacterFootSide.Left ? frame.Left : frame.Right;
            CharacterFootGroundPathDiagnostics ground = foot.GroundPath;
            CharacterFootSwingMotionDiagnostics motion = foot.FootMotion;
            Vector3 motionUp =
                motion.PathContinuity.TargetHeightComponentUp.sqrMagnitude > 0.000001f
                    ? motion.PathContinuity.TargetHeightComponentUp.normalized
                    : default;
            Vector3 groundPathUp = ground.ComponentUp.sqrMagnitude > 0.000001f
                ? ground.ComponentUp.normalized
                : default;
            float originalSoleAlongUp = Vector3.Dot(
                motion.Core.OriginalSole,
                motionUp);
            var result = new CharacterFootIkMotionCoreDerivedFacts
            {
                BaselineSampleAlongUp = Vector3.Dot(
                    motion.Core.BaselineSample,
                    motionUp),
                EnvelopeSampleAlongUp = Vector3.Dot(
                    motion.Core.EnvelopeSample,
                    motionUp)
            };
            CharacterFootStepObservationInputDiagnostics inputObservation =
                frame.Input.FootStepObservation;
            AnimationFootMotionRuntimeSample inputObservedStep =
                side == CharacterFootSide.Left
                    ? inputObservation.Left
                    : inputObservation.Right;
            result.FormalFootHeight =
                inputObservation.IsValid && inputObservedStep.IsValid
                    ? inputObservedStep.FootHeight
                    : 0f;
            result.RawFormalTargetHeight =
                result.EnvelopeSampleAlongUp + result.FormalFootHeight;
            result.EnvelopeMinimumCorrection =
                result.EnvelopeSampleAlongUp - originalSoleAlongUp;
            result.BuilderSelectedCorrection = Mathf.Max(
                0f,
                result.RawFormalTargetHeight - originalSoleAlongUp);
            result.BuilderSwingTargetAvailable =
                motion.PathContinuity.PathContinuityEvaluated &&
                motion.PathContinuity.PathAvailableAfter &&
                motion.PathContinuity.PathCurrentLandingEventIdentity ==
                motion.Core.LandingEventIdentity;
            result.BuilderSwingTargetCorrection =
                result.BuilderSwingTargetAvailable
                    ? motion.PathContinuity.PathCurrentTargetCorrection
                    : default;
            result.ActualEnvelope = ResolveActualEnvelope(
                in ground,
                in motion,
                groundPathUp);
            result.HorizontalAxisState = result.ActualEnvelope.State switch
            {
                CharacterFootIkActualEnvelopeIntersectionState.Unavailable =>
                    CharacterFootIkSwingPathHorizontalAxisState.Unavailable,
                CharacterFootIkActualEnvelopeIntersectionState.InvalidComponentUp =>
                    CharacterFootIkSwingPathHorizontalAxisState.InvalidComponentUp,
                CharacterFootIkActualEnvelopeIntersectionState.DegenerateAxis =>
                    CharacterFootIkSwingPathHorizontalAxisState.DegenerateAxis,
                _ => CharacterFootIkSwingPathHorizontalAxisState.Available
            };
            result.ActualEnvelopeCorrectionAvailable =
                result.ActualEnvelope.CounterfactualState ==
                CharacterFootIkActualEnvelopeCounterfactualState.UniqueInCorridor &&
                result.BuilderSwingTargetAvailable;
            result.ActualEnvelopeMinimumCorrection =
                result.ActualEnvelopeCorrectionAvailable
                    ? result.ActualEnvelope.MinimumHeightAlongUp -
                      originalSoleAlongUp
                    : 0f;
            float builderSwingTargetAlongUp =
                result.BuilderSwingTargetAvailable
                    ? Vector3.Dot(result.BuilderSwingTargetCorrection, motionUp)
                    : 0f;
            result.ActualEnvelopeAdvanceAboveBuilderTarget =
                result.ActualEnvelopeCorrectionAvailable
                    ? Mathf.Max(
                        0f,
                        result.ActualEnvelopeMinimumCorrection -
                        builderSwingTargetAlongUp)
                    : 0f;
            result.PenetrationAvailability = ResolvePenetrationAvailability(
                in view,
                in frame,
                in motion);
            return result;
        }

        static CharacterFootIkActualEnvelopeFacts ResolveActualEnvelope(
            in CharacterFootGroundPathDiagnostics ground,
            in CharacterFootSwingMotionDiagnostics motion,
            Vector3 up)
        {
            var result = new CharacterFootIkActualEnvelopeFacts
            {
                State = CharacterFootIkActualEnvelopeIntersectionState.Unavailable
            };
            if (!ground.Accepted ||
                motion.Core.State != CharacterFootSwingMotionState.Accepted ||
                motion.Core.ConstraintState != CharacterFootConstraintState.Swing ||
                ground.EnvelopeVertexCount < 2)
            {
                return result;
            }
            if (!Finite(up) || up.sqrMagnitude <= 0.000001f)
            {
                result.State =
                    CharacterFootIkActualEnvelopeIntersectionState.InvalidComponentUp;
                return result;
            }
            Vector3 horizontalAxis = Vector3.ProjectOnPlane(
                ground.NextSwingLanding - ground.LastLanding,
                up);
            if (!Finite(horizontalAxis) ||
                horizontalAxis.sqrMagnitude <= 0.00000001f)
            {
                result.State =
                    CharacterFootIkActualEnvelopeIntersectionState.DegenerateAxis;
                return result;
            }
            Vector3 direction = horizontalAxis.normalized;
            float pathLength = horizontalAxis.magnitude;
            Vector3 actualHorizontalOffset = Vector3.ProjectOnPlane(
                motion.Core.OriginalSole - ground.LastLanding,
                up);
            result.ActualFootHorizontalDistance = Vector3.Dot(
                actualHorizontalOffset,
                direction);
            result.BaselineHorizontalDistance = Vector3.Dot(
                motion.Core.BaselineSample - ground.LastLanding,
                direction);
            result.EnvelopeHorizontalDistance = Vector3.Dot(
                motion.Core.EnvelopeSample - ground.LastLanding,
                direction);
            float rawPathParameter =
                result.ActualFootHorizontalDistance / pathLength;
            result.AxisRegion = result.ActualFootHorizontalDistance <
                                -HorizontalEpsilonMeters
                ? CharacterFootIkActualFootAxisRegion.BeforePathStart
                : result.ActualFootHorizontalDistance >
                  pathLength + HorizontalEpsilonMeters
                    ? CharacterFootIkActualFootAxisRegion.AfterPathEnd
                    : CharacterFootIkActualFootAxisRegion.WithinPathSegment;
            result.ClosestPathParameter = Mathf.Clamp01(rawPathParameter);
            result.DistanceAlongAxis =
                result.ClosestPathParameter * pathLength;
            Vector3 closestHorizontalOffset =
                horizontalAxis * result.ClosestPathParameter;
            result.CrossTrackDistance = Vector3.Distance(
                actualHorizontalOffset,
                closestHorizontalOffset);
            result.CorridorRadius = ground.Query.Radius;
            result.WithinGroundPathCorridor =
                float.IsFinite(result.CorridorRadius) &&
                result.CorridorRadius > 0f &&
                result.CrossTrackDistance <=
                result.CorridorRadius + HorizontalEpsilonMeters;
            Span<float> heights = stackalloc float[MaximumEnvelopeHeightCount];
            int heightCount = 0;
            for (int i = 1; i < ground.EnvelopeVertexCount; i++)
            {
                CharacterFootGroundEnvelopeVertex previous =
                    ground.EnvelopeVertexAt(i - 1);
                CharacterFootGroundEnvelopeVertex current =
                    ground.EnvelopeVertexAt(i);
                float previousDistance = Vector3.Dot(
                    previous.Position - ground.LastLanding,
                    direction);
                float currentDistance = Vector3.Dot(
                    current.Position - ground.LastLanding,
                    direction);
                float minimumDistance = Mathf.Min(
                    previousDistance,
                    currentDistance);
                float maximumDistance = Mathf.Max(
                    previousDistance,
                    currentDistance);
                if (result.ActualFootHorizontalDistance <
                        minimumDistance - HorizontalEpsilonMeters ||
                    result.ActualFootHorizontalDistance >
                        maximumDistance + HorizontalEpsilonMeters)
                {
                    continue;
                }
                float previousHeight = Vector3.Dot(previous.Position, up);
                float currentHeight = Vector3.Dot(current.Position, up);
                float distanceDelta = currentDistance - previousDistance;
                if (Mathf.Abs(distanceDelta) <= HorizontalEpsilonMeters)
                {
                    if (Mathf.Abs(
                            result.ActualFootHorizontalDistance -
                            previousDistance) > HorizontalEpsilonMeters)
                    {
                        continue;
                    }
                    AddUniqueHeight(heights, ref heightCount, previousHeight);
                    AddUniqueHeight(heights, ref heightCount, currentHeight);
                    if (Mathf.Abs(currentHeight - previousHeight) >
                        HeightEpsilonMeters)
                    {
                        result.HasVerticalEdge = true;
                    }
                    continue;
                }
                float interpolation =
                    (result.ActualFootHorizontalDistance - previousDistance) /
                    distanceDelta;
                AddUniqueHeight(
                    heights,
                    ref heightCount,
                    Mathf.Lerp(
                        previousHeight,
                        currentHeight,
                        Mathf.Clamp01(interpolation)));
            }
            if (heightCount == 0)
            {
                result.State =
                    CharacterFootIkActualEnvelopeIntersectionState.NoIntersection;
                result.CounterfactualState = result.WithinGroundPathCorridor
                    ? CharacterFootIkActualEnvelopeCounterfactualState.NoIntersection
                    : CharacterFootIkActualEnvelopeCounterfactualState
                        .OutsideGroundPathCorridor;
                return result;
            }
            result.CandidateCount = heightCount;
            result.MinimumHeightAlongUp = heights[0];
            result.MaximumHeightAlongUp = heights[0];
            for (int i = 1; i < heightCount; i++)
            {
                result.MinimumHeightAlongUp = Mathf.Min(
                    result.MinimumHeightAlongUp,
                    heights[i]);
                result.MaximumHeightAlongUp = Mathf.Max(
                    result.MaximumHeightAlongUp,
                    heights[i]);
            }
            result.HeightSpan = result.MaximumHeightAlongUp -
                                result.MinimumHeightAlongUp;
            result.HasMultipleHeights = heightCount > 1 &&
                result.HeightSpan > HeightEpsilonMeters;
            result.Ambiguous = result.HasVerticalEdge ||
                               result.HasMultipleHeights;
            result.State = result.Ambiguous
                ? CharacterFootIkActualEnvelopeIntersectionState
                    .AmbiguousEnvelopeAtActualFootDistance
                : CharacterFootIkActualEnvelopeIntersectionState.Unique;
            result.CounterfactualState = !result.WithinGroundPathCorridor
                ? CharacterFootIkActualEnvelopeCounterfactualState
                    .OutsideGroundPathCorridor
                : result.Ambiguous
                    ? CharacterFootIkActualEnvelopeCounterfactualState
                        .AmbiguousInCorridor
                    : CharacterFootIkActualEnvelopeCounterfactualState
                        .UniqueInCorridor;
            return result;
        }

        static void AddUniqueHeight(
            Span<float> heights,
            ref int count,
            float value)
        {
            if (!float.IsFinite(value))
                return;
            for (int i = 0; i < count; i++)
            {
                if (Mathf.Abs(heights[i] - value) <= HeightEpsilonMeters)
                    return;
            }
            heights[count++] = value;
        }

        static CharacterFootIkContactPlanePenetrationAvailability
            ResolvePenetrationAvailability(
                in CharacterFootIkCommittedCaptureViewLease view,
                in CharacterFootLandingPredictionDiagnostics frame,
                in CharacterFootSwingMotionDiagnostics motion)
        {
            if (!view.PhysicalWriteAvailable ||
                view.PhysicalWriteCompletionIdentity != frame.CompletionIdentity)
            {
                return CharacterFootIkContactPlanePenetrationAvailability
                    .FinalPhysicalPoseUnavailable;
            }
            if (motion.Core.ConstraintState != CharacterFootConstraintState.Landing &&
                motion.Core.ConstraintState != CharacterFootConstraintState.Locked)
            {
                return CharacterFootIkContactPlanePenetrationAvailability
                    .ContactLifecycleUnavailable;
            }
            if (!motion.Core.ContactPlaneAvailable)
            {
                return CharacterFootIkContactPlanePenetrationAvailability
                    .ContactPlaneUnavailable;
            }
            if (motion.Core.LandingEventIdentity == 0)
            {
                return CharacterFootIkContactPlanePenetrationAvailability
                    .EventLineageMismatch;
            }
            if (motion.Core.ContactSurfaceIdentity == 0)
            {
                return CharacterFootIkContactPlanePenetrationAvailability
                    .SurfaceLineageMismatch;
            }
            Vector3 normal = motion.Core.ContactPlaneNormal;
            if (!Finite(normal) || normal.sqrMagnitude <= 0.000001f)
            {
                return CharacterFootIkContactPlanePenetrationAvailability
                    .InvalidContactNormal;
            }
            return CharacterFootIkContactPlanePenetrationAvailability.Available;
        }

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) &&
            float.IsFinite(value.y) &&
            float.IsFinite(value.z);
    }

    internal static partial class CharacterFootIkDiagnosticFields
    {
        const string MotionDerivedGroup = "motion-derived";
        const string BuilderSwingTargetAvailableField =
            "character-foot-ik/main/foot-motion-builder-swing-target-available";
        const string ActualEnvelopeCorrectionAvailableField =
            "character-foot-ik/main/foot-motion-actual-progress-envelope-correction-available";

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-baseline-sample-along-up", 1, DiagnosticValueKind.Float32, "metres", Main, MotionDerivedGroup, Derived = true, Dependencies = new[] { "character-foot-ik/main/foot-motion-baseline-sample", "character-foot-ik/main/foot-motion-target-height-component-up" })]
        internal static float FootMotionBaselineSampleAlongUp(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => metadata.MotionCoreDerived.BaselineSampleAlongUp;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-envelope-sample-along-up", 1, DiagnosticValueKind.Float32, "metres", Main, MotionDerivedGroup, Derived = true, Dependencies = new[] { "character-foot-ik/main/foot-motion-envelope-sample", "character-foot-ik/main/foot-motion-target-height-component-up" })]
        internal static float FootMotionEnvelopeSampleAlongUp(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => metadata.MotionCoreDerived.EnvelopeSampleAlongUp;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-formal-foot-height", 1, DiagnosticValueKind.Float32, "metres", Main, MotionDerivedGroup, Derived = true, Dependencies = new[] { "character-foot-ik/main/input-formal-foot-height" })]
        internal static float FootMotionFormalFootHeight(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => metadata.MotionCoreDerived.FormalFootHeight;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-raw-formal-target-height", 1, DiagnosticValueKind.Float32, "metres", Main, MotionDerivedGroup, Derived = true, Dependencies = new[] { "character-foot-ik/main/foot-motion-envelope-sample-along-up", "character-foot-ik/main/foot-motion-formal-foot-height" })]
        internal static float FootMotionRawFormalTargetHeight(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => metadata.MotionCoreDerived.RawFormalTargetHeight;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-envelope-minimum-correction", 1, DiagnosticValueKind.Float32, "metres", Main, MotionDerivedGroup, Derived = true, Dependencies = new[] { "character-foot-ik/main/foot-motion-envelope-sample-along-up", "character-foot-ik/main/foot-motion-original-sole", "character-foot-ik/main/foot-motion-target-height-component-up" })]
        internal static float FootMotionEnvelopeMinimumCorrection(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => metadata.MotionCoreDerived.EnvelopeMinimumCorrection;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-builder-selected-correction", 1, DiagnosticValueKind.Float32, "metres", Main, MotionDerivedGroup, Derived = true, Dependencies = new[] { "character-foot-ik/main/foot-motion-raw-formal-target-height", "character-foot-ik/main/foot-motion-original-sole", "character-foot-ik/main/foot-motion-target-height-component-up" })]
        internal static float FootMotionBuilderSelectedCorrection(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => metadata.MotionCoreDerived.BuilderSelectedCorrection;

        [DiagnosticField(Capability, BuilderSwingTargetAvailableField, 1, DiagnosticValueKind.Boolean, "none", Main, MotionDerivedGroup, Derived = true, Dependencies = new[] { "character-foot-ik/main/foot-motion-path-continuity-evaluated", "character-foot-ik/main/foot-motion-path-available-after", "character-foot-ik/main/foot-motion-path-current-landing-event-identity", "character-foot-ik/main/foot-motion-landing-event-identity" })]
        internal static bool FootMotionBuilderSwingTargetAvailable(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => metadata.MotionCoreDerived.BuilderSwingTargetAvailable;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-builder-swing-target-correction", 1, DiagnosticValueKind.Vector3, "metres", Main, MotionDerivedGroup, AvailabilityFieldId = BuilderSwingTargetAvailableField, Derived = true, Dependencies = new[] { "character-foot-ik/main/foot-motion-path-current-target-correction" })]
        internal static DiagnosticVector3 FootMotionBuilderSwingTargetCorrection(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(metadata.MotionCoreDerived.BuilderSwingTargetCorrection);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-swing-path-horizontal-axis-state", 1, DiagnosticValueKind.Int32, "category", Main, MotionDerivedGroup, Derived = true, Dependencies = new[] { "character-foot-ik/main/ground-path-input-identity", "character-foot-ik/main/ground-envelope-vertex-count", "character-foot-ik/main/ground-path-component-up" })]
        internal static int FootMotionSwingPathHorizontalAxisState(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)metadata.MotionCoreDerived.HorizontalAxisState;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-actual-foot-horizontal-distance-meters", 1, DiagnosticValueKind.Float32, "metres", Main, MotionDerivedGroup, Derived = true, Dependencies = new[] { "character-foot-ik/main/ground-path-input-identity", "character-foot-ik/main/foot-motion-original-sole" })]
        internal static float FootMotionActualFootHorizontalDistanceMeters(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => metadata.MotionCoreDerived.ActualEnvelope.ActualFootHorizontalDistance;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-baseline-horizontal-distance-meters", 1, DiagnosticValueKind.Float32, "metres", Main, MotionDerivedGroup, Derived = true, Dependencies = new[] { "character-foot-ik/main/ground-path-input-identity", "character-foot-ik/main/foot-motion-baseline-sample" })]
        internal static float FootMotionBaselineHorizontalDistanceMeters(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => metadata.MotionCoreDerived.ActualEnvelope.BaselineHorizontalDistance;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-envelope-horizontal-distance-meters", 1, DiagnosticValueKind.Float32, "metres", Main, MotionDerivedGroup, Derived = true, Dependencies = new[] { "character-foot-ik/main/ground-path-input-identity", "character-foot-ik/main/foot-motion-envelope-sample" })]
        internal static float FootMotionEnvelopeHorizontalDistanceMeters(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => metadata.MotionCoreDerived.ActualEnvelope.EnvelopeHorizontalDistance;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-actual-minus-envelope-horizontal-distance-meters", 1, DiagnosticValueKind.Float32, "metres", Main, MotionDerivedGroup, Derived = true, Dependencies = new[] { "character-foot-ik/main/foot-motion-actual-foot-horizontal-distance-meters", "character-foot-ik/main/foot-motion-envelope-horizontal-distance-meters" })]
        internal static float FootMotionActualMinusEnvelopeHorizontalDistanceMeters(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => metadata.MotionCoreDerived.ActualEnvelope.ActualFootHorizontalDistance - metadata.MotionCoreDerived.ActualEnvelope.EnvelopeHorizontalDistance;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-actual-foot-axis-region", 1, DiagnosticValueKind.Int32, "category", Main, MotionDerivedGroup, Derived = true, Dependencies = new[] { "character-foot-ik/main/foot-motion-actual-foot-horizontal-distance-meters", "character-foot-ik/main/ground-path-input-identity" })]
        internal static int FootMotionActualFootAxisRegion(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)metadata.MotionCoreDerived.ActualEnvelope.AxisRegion;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-actual-foot-closest-path-parameter", 1, DiagnosticValueKind.Float32, "unitless", Main, MotionDerivedGroup, Derived = true, Dependencies = new[] { "character-foot-ik/main/foot-motion-actual-foot-horizontal-distance-meters", "character-foot-ik/main/ground-path-input-identity" })]
        internal static float FootMotionActualFootClosestPathParameter(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => metadata.MotionCoreDerived.ActualEnvelope.ClosestPathParameter;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-actual-foot-distance-along-axis-meters", 1, DiagnosticValueKind.Float32, "metres", Main, MotionDerivedGroup, Derived = true, Dependencies = new[] { "character-foot-ik/main/foot-motion-actual-foot-closest-path-parameter", "character-foot-ik/main/ground-path-input-identity" })]
        internal static float FootMotionActualFootDistanceAlongAxisMeters(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => metadata.MotionCoreDerived.ActualEnvelope.DistanceAlongAxis;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-actual-foot-cross-track-distance-meters", 1, DiagnosticValueKind.Float32, "metres", Main, MotionDerivedGroup, Derived = true, Dependencies = new[] { "character-foot-ik/main/foot-motion-actual-foot-closest-path-parameter", "character-foot-ik/main/foot-motion-original-sole" })]
        internal static float FootMotionActualFootCrossTrackDistanceMeters(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => metadata.MotionCoreDerived.ActualEnvelope.CrossTrackDistance;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-actual-foot-ground-path-corridor-radius-meters", 1, DiagnosticValueKind.Float32, "metres", Main, MotionDerivedGroup, Derived = true, Dependencies = new[] { "character-foot-ik/main/ground-path-radius" })]
        internal static float FootMotionActualFootGroundPathCorridorRadiusMeters(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => metadata.MotionCoreDerived.ActualEnvelope.CorridorRadius;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-actual-foot-within-ground-path-corridor", 1, DiagnosticValueKind.Boolean, "none", Main, MotionDerivedGroup, Derived = true, Dependencies = new[] { "character-foot-ik/main/foot-motion-actual-foot-cross-track-distance-meters", "character-foot-ik/main/foot-motion-actual-foot-ground-path-corridor-radius-meters" })]
        internal static bool FootMotionActualFootWithinGroundPathCorridor(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => metadata.MotionCoreDerived.ActualEnvelope.WithinGroundPathCorridor;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-actual-envelope-intersection-state", 1, DiagnosticValueKind.Int32, "category", Main, MotionDerivedGroup, Derived = true, Dependencies = new[] { "character-foot-ik/main/ground-envelope-vertex-count", "character-foot-ik/main/foot-motion-swing-path-horizontal-axis-state" })]
        internal static int FootMotionActualEnvelopeIntersectionState(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)metadata.MotionCoreDerived.ActualEnvelope.State;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-actual-envelope-candidate-count", 1, DiagnosticValueKind.Int32, "count", Main, MotionDerivedGroup, Derived = true, Dependencies = new[] { "character-foot-ik/main/foot-motion-actual-envelope-intersection-state" })]
        internal static int FootMotionActualEnvelopeCandidateCount(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => metadata.MotionCoreDerived.ActualEnvelope.CandidateCount;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-actual-envelope-minimum-height-along-up", 1, DiagnosticValueKind.Float32, "metres", Main, MotionDerivedGroup, Derived = true, Dependencies = new[] { "character-foot-ik/main/foot-motion-actual-envelope-intersection-state" })]
        internal static float FootMotionActualEnvelopeMinimumHeightAlongUp(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => metadata.MotionCoreDerived.ActualEnvelope.MinimumHeightAlongUp;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-actual-envelope-maximum-height-along-up", 1, DiagnosticValueKind.Float32, "metres", Main, MotionDerivedGroup, Derived = true, Dependencies = new[] { "character-foot-ik/main/foot-motion-actual-envelope-intersection-state" })]
        internal static float FootMotionActualEnvelopeMaximumHeightAlongUp(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => metadata.MotionCoreDerived.ActualEnvelope.MaximumHeightAlongUp;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-actual-envelope-height-span", 1, DiagnosticValueKind.Float32, "metres", Main, MotionDerivedGroup, Derived = true, Dependencies = new[] { "character-foot-ik/main/foot-motion-actual-envelope-minimum-height-along-up", "character-foot-ik/main/foot-motion-actual-envelope-maximum-height-along-up" })]
        internal static float FootMotionActualEnvelopeHeightSpan(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => metadata.MotionCoreDerived.ActualEnvelope.HeightSpan;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-actual-envelope-has-vertical-edge", 1, DiagnosticValueKind.Boolean, "none", Main, MotionDerivedGroup, Derived = true, Dependencies = new[] { "character-foot-ik/main/foot-motion-actual-envelope-intersection-state" })]
        internal static bool FootMotionActualEnvelopeHasVerticalEdge(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => metadata.MotionCoreDerived.ActualEnvelope.HasVerticalEdge;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-actual-envelope-has-multiple-heights", 1, DiagnosticValueKind.Boolean, "none", Main, MotionDerivedGroup, Derived = true, Dependencies = new[] { "character-foot-ik/main/foot-motion-actual-envelope-height-span", "character-foot-ik/main/foot-motion-actual-envelope-candidate-count" })]
        internal static bool FootMotionActualEnvelopeHasMultipleHeights(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => metadata.MotionCoreDerived.ActualEnvelope.HasMultipleHeights;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-actual-envelope-ambiguous", 1, DiagnosticValueKind.Boolean, "none", Main, MotionDerivedGroup, Derived = true, Dependencies = new[] { "character-foot-ik/main/foot-motion-actual-envelope-has-vertical-edge", "character-foot-ik/main/foot-motion-actual-envelope-has-multiple-heights" })]
        internal static bool FootMotionActualEnvelopeAmbiguous(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => metadata.MotionCoreDerived.ActualEnvelope.Ambiguous;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-actual-envelope-counterfactual-state", 1, DiagnosticValueKind.Int32, "category", Main, MotionDerivedGroup, Derived = true, Dependencies = new[] { "character-foot-ik/main/foot-motion-actual-envelope-intersection-state", "character-foot-ik/main/foot-motion-actual-foot-within-ground-path-corridor" })]
        internal static int FootMotionActualEnvelopeCounterfactualState(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)metadata.MotionCoreDerived.ActualEnvelope.CounterfactualState;

        [DiagnosticField(Capability, ActualEnvelopeCorrectionAvailableField, 1, DiagnosticValueKind.Boolean, "none", Main, MotionDerivedGroup, Derived = true, Dependencies = new[] { "character-foot-ik/main/foot-motion-actual-envelope-counterfactual-state", BuilderSwingTargetAvailableField })]
        internal static bool FootMotionActualProgressEnvelopeCorrectionAvailable(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => metadata.MotionCoreDerived.ActualEnvelopeCorrectionAvailable;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-actual-progress-envelope-minimum-correction", 1, DiagnosticValueKind.Float32, "unitless", Main, MotionDerivedGroup, AvailabilityFieldId = ActualEnvelopeCorrectionAvailableField, Derived = true, Dependencies = new[] { "character-foot-ik/main/foot-motion-actual-envelope-minimum-height-along-up", "character-foot-ik/main/foot-motion-original-sole", "character-foot-ik/main/foot-motion-target-height-component-up" })]
        internal static float FootMotionActualProgressEnvelopeMinimumCorrection(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => metadata.MotionCoreDerived.ActualEnvelopeMinimumCorrection;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-actual-progress-envelope-advance-above-builder-target", 1, DiagnosticValueKind.Float32, "unitless", Main, MotionDerivedGroup, AvailabilityFieldId = ActualEnvelopeCorrectionAvailableField, Derived = true, Dependencies = new[] { "character-foot-ik/main/foot-motion-actual-progress-envelope-minimum-correction", "character-foot-ik/main/foot-motion-builder-swing-target-correction", "character-foot-ik/main/foot-motion-target-height-component-up" })]
        internal static float FootMotionActualProgressEnvelopeAdvanceAboveBuilderTarget(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => metadata.MotionCoreDerived.ActualEnvelopeAdvanceAboveBuilderTarget;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-contact-plane-penetration-availability", 1, DiagnosticValueKind.Int32, "category", Main, MotionDerivedGroup, Derived = true, Dependencies = new[] { "character-foot-ik/main/completion-identity", "character-foot-ik/main/foot-motion-constraint-state", "character-foot-ik/main/foot-motion-contact-plane-available", "character-foot-ik/main/foot-motion-landing-event-identity", "character-foot-ik/main/foot-motion-contact-surface-identity" })]
        internal static int FootContactPlanePenetrationAvailability(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)metadata.MotionCoreDerived.PenetrationAvailability;
    }
}
