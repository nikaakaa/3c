using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum AnimationPhaseSourceKind : byte
    {
        DirectClip = 1,
        BlendSpace = 2
    }

    [Serializable]
    public struct AnimationPhaseCoverage
    {
        [SerializeField] float m_StartSeconds;
        [SerializeField] float m_EndSeconds;

        public AnimationPhaseCoverage(float startSeconds, float endSeconds)
        {
            if (!float.IsFinite(startSeconds) || !float.IsFinite(endSeconds) ||
                startSeconds < 0f || endSeconds <= startSeconds)
            {
                throw new ArgumentException("Animation Phase coverage is invalid.");
            }
            m_StartSeconds = startSeconds;
            m_EndSeconds = endSeconds;
        }

        public float StartSeconds => m_StartSeconds;
        public float EndSeconds => m_EndSeconds;
        public bool IsValid => float.IsFinite(StartSeconds) && float.IsFinite(EndSeconds) &&
                               StartSeconds >= 0f && EndSeconds > StartSeconds;
        public bool Contains(double time) => IsValid &&
                                             double.IsFinite(time) &&
                                             time >= StartSeconds && time <= EndSeconds;
    }

    [Serializable]
    public struct AnimationPhaseKnot
    {
        [SerializeField] float m_TimeSeconds;
        [SerializeField] float m_UnwrappedPhase;

        public AnimationPhaseKnot(float timeSeconds, float unwrappedPhase)
        {
            if (!float.IsFinite(timeSeconds) || !float.IsFinite(unwrappedPhase) || timeSeconds < 0f)
                throw new ArgumentException("Animation Phase knot is invalid.");
            m_TimeSeconds = timeSeconds;
            m_UnwrappedPhase = unwrappedPhase;
        }

        public float TimeSeconds => m_TimeSeconds;
        public float UnwrappedPhase => m_UnwrappedPhase;
    }

    [Serializable]
    public sealed class AnimationClipPhasePlan
    {
        public const string SchemaVersion = "animation-clip-phase-plan/v1";

        [SerializeField] string m_SchemaVersion = SchemaVersion;
        [SerializeField] string m_ClipIdentity = string.Empty;
        [SerializeField] string m_FullClipDependencyHash = string.Empty;
        [SerializeField] string m_AnalysisInputHash = string.Empty;
        [SerializeField] string m_RegisteredCurveHash = string.Empty;
        [SerializeField] string m_ValidationIdentity = string.Empty;
        [SerializeField] float m_SourceDurationSeconds;
        [SerializeField] AnimationPhaseCoverage m_CurveCoverage;
        [SerializeField] bool m_Loop;
        [SerializeField] AnimationPhaseKnot[] m_Knots = Array.Empty<AnimationPhaseKnot>();

        public AnimationClipPhasePlan(
            string clipIdentity,
            string fullClipDependencyHash,
            string analysisInputHash,
            string registeredCurveHash,
            string validationIdentity,
            float sourceDurationSeconds,
            AnimationPhaseCoverage curveCoverage,
            bool loop,
            AnimationPhaseKnot[] knots)
        {
            m_ClipIdentity = clipIdentity?.Trim() ?? string.Empty;
            m_FullClipDependencyHash = fullClipDependencyHash?.Trim() ?? string.Empty;
            m_AnalysisInputHash = analysisInputHash?.Trim() ?? string.Empty;
            m_RegisteredCurveHash = registeredCurveHash?.Trim() ?? string.Empty;
            m_ValidationIdentity = validationIdentity?.Trim() ?? string.Empty;
            m_SourceDurationSeconds = sourceDurationSeconds;
            m_CurveCoverage = curveCoverage;
            m_Loop = loop;
            m_Knots = knots == null ? Array.Empty<AnimationPhaseKnot>() : (AnimationPhaseKnot[])knots.Clone();
            RequireValid();
        }

        public string ClipIdentity => m_ClipIdentity ?? string.Empty;
        public string FullClipDependencyHash => m_FullClipDependencyHash ?? string.Empty;
        public string AnalysisInputHash => m_AnalysisInputHash ?? string.Empty;
        public string RegisteredCurveHash => m_RegisteredCurveHash ?? string.Empty;
        public string ValidationIdentity => m_ValidationIdentity ?? string.Empty;
        public float SourceDurationSeconds => m_SourceDurationSeconds;
        public AnimationPhaseCoverage CurveCoverage => m_CurveCoverage;
        public bool Loop => m_Loop;
        public IReadOnlyList<AnimationPhaseKnot> Knots => m_Knots ?? Array.Empty<AnimationPhaseKnot>();
        public float PhaseStart => Knots[0].UnwrappedPhase;
        public float PhaseEnd => Knots[Knots.Count - 1].UnwrappedPhase;
        public float PhaseSpan => PhaseEnd - PhaseStart;

        public void RequireValid()
        {
            if (!string.Equals(m_SchemaVersion, SchemaVersion, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(ClipIdentity) ||
                string.IsNullOrWhiteSpace(FullClipDependencyHash) ||
                string.IsNullOrWhiteSpace(AnalysisInputHash) ||
                string.IsNullOrWhiteSpace(RegisteredCurveHash) ||
                string.IsNullOrWhiteSpace(ValidationIdentity) ||
                !float.IsFinite(SourceDurationSeconds) || SourceDurationSeconds <= 0f ||
                !CurveCoverage.IsValid || Knots.Count < 2)
            {
                throw new InvalidOperationException("Animation Clip Phase plan is incomplete.");
            }
            for (int i = 0; i < Knots.Count; i++)
            {
                AnimationPhaseKnot knot = Knots[i];
                if (knot.TimeSeconds < CurveCoverage.StartSeconds ||
                    knot.TimeSeconds > CurveCoverage.EndSeconds ||
                    i > 0 && (knot.TimeSeconds <= Knots[i - 1].TimeSeconds ||
                              knot.UnwrappedPhase <= Knots[i - 1].UnwrappedPhase))
                {
                    throw new InvalidOperationException($"Animation Clip Phase knot #{i} is invalid.");
                }
            }
            if (Mathf.Abs(Knots[0].TimeSeconds - CurveCoverage.StartSeconds) > 0.00001f ||
                Mathf.Abs(Knots[Knots.Count - 1].TimeSeconds - CurveCoverage.EndSeconds) > 0.00001f ||
                Loop && (Mathf.Abs(CurveCoverage.StartSeconds) > 0.00001f ||
                         Mathf.Abs(CurveCoverage.EndSeconds - SourceDurationSeconds) > 0.00001f ||
                         PhaseSpan < 1f || Mathf.Abs(PhaseSpan - Mathf.Round(PhaseSpan)) > 0.0001f))
            {
                throw new InvalidOperationException("Animation Clip Phase coverage is inconsistent.");
            }
        }

        public double Forward(double continuousTime)
        {
            RequireValid();
            if (!double.IsFinite(continuousTime) || continuousTime < 0d)
                throw new ArgumentOutOfRangeException(nameof(continuousTime));
            if (!Loop)
            {
                if (!CurveCoverage.Contains(continuousTime))
                    throw new InvalidOperationException("Finite Animation Phase time is outside coverage.");
                return InterpolateForward((float)continuousTime);
            }
            double cycle = Math.Floor(continuousTime / SourceDurationSeconds);
            double local = continuousTime - cycle * SourceDurationSeconds;
            if (local >= SourceDurationSeconds)
            {
                local = 0d;
                cycle += 1d;
            }
            return InterpolateForward((float)local) + cycle * PhaseSpan;
        }

        public double Inverse(double unwrappedPhase, double rawContinuationTime)
        {
            RequireValid();
            if (!double.IsFinite(unwrappedPhase) || !double.IsFinite(rawContinuationTime) || rawContinuationTime < 0d)
                throw new ArgumentOutOfRangeException();
            if (!Loop)
            {
                if (unwrappedPhase < PhaseStart || unwrappedPhase > PhaseEnd)
                    throw new InvalidOperationException("Finite Animation Phase is outside coverage.");
                return InterpolateInverse((float)unwrappedPhase);
            }
            double continuationPhase = Forward(rawContinuationTime);
            double adjusted = unwrappedPhase + Math.Round((continuationPhase - unwrappedPhase) / PhaseSpan) * PhaseSpan;
            double cycle = Math.Floor((adjusted - PhaseStart) / PhaseSpan);
            double localPhase = adjusted - cycle * PhaseSpan;
            if (localPhase < PhaseStart)
            {
                localPhase += PhaseSpan;
                cycle -= 1d;
            }
            else if (localPhase > PhaseEnd)
            {
                localPhase -= PhaseSpan;
                cycle += 1d;
            }
            return cycle * SourceDurationSeconds + InterpolateInverse((float)localPhase);
        }

        float InterpolateForward(float time)
        {
            int upper = UpperTime(time);
            AnimationPhaseKnot left = Knots[upper - 1];
            AnimationPhaseKnot right = Knots[upper];
            float t = Mathf.InverseLerp(left.TimeSeconds, right.TimeSeconds, time);
            return Mathf.LerpUnclamped(left.UnwrappedPhase, right.UnwrappedPhase, t);
        }

        float InterpolateInverse(float phase)
        {
            int upper = UpperPhase(phase);
            AnimationPhaseKnot left = Knots[upper - 1];
            AnimationPhaseKnot right = Knots[upper];
            float t = Mathf.InverseLerp(left.UnwrappedPhase, right.UnwrappedPhase, phase);
            return Mathf.LerpUnclamped(left.TimeSeconds, right.TimeSeconds, t);
        }

        int UpperTime(float value)
        {
            if (value <= Knots[0].TimeSeconds)
                return 1;
            for (int i = 1; i < Knots.Count; i++)
            {
                if (value <= Knots[i].TimeSeconds)
                    return i;
            }
            return Knots.Count - 1;
        }

        int UpperPhase(float value)
        {
            if (value <= Knots[0].UnwrappedPhase)
                return 1;
            for (int i = 1; i < Knots.Count; i++)
            {
                if (value <= Knots[i].UnwrappedPhase)
                    return i;
            }
            return Knots.Count - 1;
        }
    }

    [Serializable]
    public sealed class AnimationSourcePhasePlan
    {
        [SerializeField] int m_SourceIndex = -1;
        [SerializeField] AnimationPhaseSourceKind m_SourceKind;
        [SerializeField] int m_ClockCarrierClipPlanIndex = -1;
        [SerializeField] int[] m_DynamicSampleClipPlanIndices = Array.Empty<int>();
        [SerializeField] AnimationPhaseCoverage m_ActualCoverage;

        public AnimationSourcePhasePlan(
            PresentationPoseSourceIndex sourceIndex,
            AnimationPhaseSourceKind sourceKind,
            int clockCarrierClipPlanIndex,
            int[] dynamicSampleClipPlanIndices,
            AnimationPhaseCoverage actualCoverage)
        {
            if (!sourceIndex.IsValid || !Enum.IsDefined(typeof(AnimationPhaseSourceKind), sourceKind) ||
                clockCarrierClipPlanIndex < 0 || !actualCoverage.IsValid)
            {
                throw new ArgumentException("Animation source Phase plan is invalid.");
            }
            m_SourceIndex = sourceIndex.Value;
            m_SourceKind = sourceKind;
            m_ClockCarrierClipPlanIndex = clockCarrierClipPlanIndex;
            m_DynamicSampleClipPlanIndices = dynamicSampleClipPlanIndices == null
                ? Array.Empty<int>()
                : (int[])dynamicSampleClipPlanIndices.Clone();
            m_ActualCoverage = actualCoverage;
        }

        public PresentationPoseSourceIndex SourceIndex => m_SourceIndex < 0
            ? default
            : new PresentationPoseSourceIndex(m_SourceIndex);
        public AnimationPhaseSourceKind SourceKind => m_SourceKind;
        public int ClockCarrierClipPlanIndex => m_ClockCarrierClipPlanIndex;
        public IReadOnlyList<int> DynamicSampleClipPlanIndices =>
            m_DynamicSampleClipPlanIndices ?? Array.Empty<int>();
        public AnimationPhaseCoverage ActualCoverage => m_ActualCoverage;
    }

    [Serializable]
    public sealed class AnimationPhaseRelationPlan
    {
        [SerializeField] string m_RelationIdentity = string.Empty;
        [SerializeField] string m_TransitionId = string.Empty;
        [SerializeField] int m_SourcePhasePlanIndex = -1;
        [SerializeField] int m_TargetPhasePlanIndex = -1;
        [SerializeField] bool m_SourceIsLeader;
        [SerializeField] CharacterClipPlayerClockSource m_LeaderClockAuthority;
        [SerializeField] string m_ValidationIdentity = string.Empty;

        public AnimationPhaseRelationPlan(
            string relationIdentity,
            PoseStateTransitionId transitionId,
            int sourcePhasePlanIndex,
            int targetPhasePlanIndex,
            bool sourceIsLeader,
            CharacterClipPlayerClockSource leaderClockAuthority,
            string validationIdentity)
        {
            if (string.IsNullOrWhiteSpace(relationIdentity) || !transitionId.IsValid ||
                sourcePhasePlanIndex < 0 || targetPhasePlanIndex < 0 ||
                !Enum.IsDefined(typeof(CharacterClipPlayerClockSource), leaderClockAuthority) ||
                string.IsNullOrWhiteSpace(validationIdentity))
            {
                throw new ArgumentException("Animation Phase relation plan is invalid.");
            }
            m_RelationIdentity = relationIdentity.Trim();
            m_TransitionId = transitionId.Value;
            m_SourcePhasePlanIndex = sourcePhasePlanIndex;
            m_TargetPhasePlanIndex = targetPhasePlanIndex;
            m_SourceIsLeader = sourceIsLeader;
            m_LeaderClockAuthority = leaderClockAuthority;
            m_ValidationIdentity = validationIdentity.Trim();
        }

        public string RelationIdentity => m_RelationIdentity ?? string.Empty;
        public PoseStateTransitionId TransitionId => string.IsNullOrWhiteSpace(m_TransitionId)
            ? default
            : new PoseStateTransitionId(m_TransitionId);
        public int SourcePhasePlanIndex => m_SourcePhasePlanIndex;
        public int TargetPhasePlanIndex => m_TargetPhasePlanIndex;
        public bool SourceIsLeader => m_SourceIsLeader;
        public CharacterClipPlayerClockSource LeaderClockAuthority => m_LeaderClockAuthority;
        public string ValidationIdentity => m_ValidationIdentity ?? string.Empty;
    }
}
