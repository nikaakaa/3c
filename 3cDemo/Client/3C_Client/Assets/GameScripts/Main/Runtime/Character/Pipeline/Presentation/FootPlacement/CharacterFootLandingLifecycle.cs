using System;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal enum CharacterFootLandingLifecycleState : byte
    {
        Empty = 0,
        Tracking = 1,
        Accepted = 2
    }

    readonly struct CharacterFootLandingFact
    {
        CharacterFootLandingFact(
            ulong landingEventIdentity,
            ulong trajectoryGeneration,
            string futureBodyTranslationSourceIdentity,
            int surfaceIdentity,
            Vector3 worldPoint,
            Vector3 worldNormal)
        {
            HasValue = true;
            LandingEventIdentity = landingEventIdentity;
            TrajectoryGeneration = trajectoryGeneration;
            FutureBodyTranslationSourceIdentity = futureBodyTranslationSourceIdentity;
            SurfaceIdentity = surfaceIdentity;
            WorldPoint = worldPoint;
            WorldNormal = worldNormal;
        }

        internal bool HasValue { get; }
        internal ulong LandingEventIdentity { get; }
        internal ulong TrajectoryGeneration { get; }
        internal string FutureBodyTranslationSourceIdentity { get; }
        internal int SurfaceIdentity { get; }
        internal Vector3 WorldPoint { get; }
        internal Vector3 WorldNormal { get; }

        internal CharacterFootGroundPathLanding Resolve() =>
            new CharacterFootGroundPathLanding(
                LandingEventIdentity,
                TrajectoryGeneration,
                FutureBodyTranslationSourceIdentity,
                SurfaceIdentity,
                WorldPoint,
                WorldNormal);

        internal static CharacterFootLandingFact Create(
            in AnimationBiomechanicalStepHeader step,
            in CharacterFootLandingPredictionFootDiagnostics diagnostics) =>
            new CharacterFootLandingFact(
                step.LandingEventIdentity,
                diagnostics.TrajectoryGeneration,
                diagnostics.FutureBodyTranslationSourceIdentity,
                diagnostics.SurfaceIdentity,
                diagnostics.LandingPoint,
                diagnostics.LandingNormal);
    }

    readonly struct CharacterFootLandingSnapshot
    {
        internal CharacterFootLandingSnapshot(
            CharacterFootLandingLifecycleState state,
            ulong eventIdentity,
            bool hasLastLanding,
            CharacterFootGroundPathLanding lastLanding,
            bool hasNextSwingLanding,
            CharacterFootGroundPathLanding nextSwingLanding,
            bool hasCompletionCandidate,
            CharacterFootGroundPathLanding completionCandidate,
            float nextSwingPredictionError,
            float nextSwingConstraintWeight)
        {
            State = state;
            EventIdentity = eventIdentity;
            HasLastLanding = hasLastLanding;
            LastLanding = lastLanding;
            HasNextSwingLanding = hasNextSwingLanding;
            NextSwingLanding = nextSwingLanding;
            HasCompletionCandidate = hasCompletionCandidate;
            CompletionCandidate = completionCandidate;
            NextSwingPredictionError = nextSwingPredictionError;
            NextSwingConstraintWeight = nextSwingConstraintWeight;
        }

        internal CharacterFootLandingLifecycleState State { get; }
        internal ulong EventIdentity { get; }
        internal bool HasLastLanding { get; }
        internal CharacterFootGroundPathLanding LastLanding { get; }
        internal ulong LastLandingEventIdentity =>
            HasLastLanding ? LastLanding.LandingEventIdentity : 0;
        internal bool HasNextSwingLanding { get; }
        internal CharacterFootGroundPathLanding NextSwingLanding { get; }
        internal bool HasCompletionCandidate { get; }
        internal CharacterFootGroundPathLanding CompletionCandidate { get; }
        internal float NextSwingPredictionError { get; }
        internal float NextSwingConstraintWeight { get; }
    }

    struct CharacterFootLandingLifecycleFrame
    {
        internal CharacterFootLandingFact LastLanding;
        internal CharacterFootLandingFact NextSwingLanding;
        internal Vector3 NextSwingReferencePoint;
        internal float NextSwingPredictionError;
        internal float NextSwingConstraintWeight;
        internal ulong ObservedCurrentEventIdentity;
        internal ulong TrackedEventIdentity;
        internal CharacterFootLandingLifecycleState State;

        internal CharacterFootLandingSnapshot Snapshot =>
            new CharacterFootLandingSnapshot(
                State,
                TrackedEventIdentity,
                LastLanding.HasValue,
                LastLanding.HasValue ? LastLanding.Resolve() : default,
                State == CharacterFootLandingLifecycleState.Accepted &&
                NextSwingLanding.HasValue,
                State == CharacterFootLandingLifecycleState.Accepted &&
                NextSwingLanding.HasValue ? NextSwingLanding.Resolve() : default,
                NextSwingLanding.HasValue,
                NextSwingLanding.HasValue ? NextSwingLanding.Resolve() : default,
                State == CharacterFootLandingLifecycleState.Accepted &&
                NextSwingLanding.HasValue ? NextSwingPredictionError : 0f,
                State == CharacterFootLandingLifecycleState.Accepted &&
                NextSwingLanding.HasValue ? NextSwingConstraintWeight : 0f);

        internal void InvalidateCurrentLanding()
        {
            NextSwingPredictionError = 0f;
            NextSwingConstraintWeight = 0f;
            State = TrackedEventIdentity != 0
                ? CharacterFootLandingLifecycleState.Tracking
                : CharacterFootLandingLifecycleState.Empty;
        }

        internal void ClearNextSwingLanding()
        {
            NextSwingLanding = default;
            NextSwingReferencePoint = default;
            NextSwingPredictionError = 0f;
            NextSwingConstraintWeight = 0f;
            State = TrackedEventIdentity != 0
                ? CharacterFootLandingLifecycleState.Tracking
                : CharacterFootLandingLifecycleState.Empty;
        }
    }

    sealed class CharacterFootLandingLifecycle
    {
        CharacterFootLandingLifecycleFrame m_Committed;
        CharacterFootLandingLifecycleFrame m_Pending;
        bool m_HasPending;

        internal CharacterFootLandingSnapshot Pending
        {
            get
            {
                RequirePending();
                return m_Pending.Snapshot;
            }
        }

        internal void BeginPending()
        {
            m_Pending = m_Committed;
            m_HasPending = true;
        }

        internal void CaptureNextSwing(
            in AnimationBiomechanicalStepHeader step,
            in CharacterFootLandingPredictionFootDiagnostics diagnostics,
            in CharacterFootMotionSettings settings)
        {
            RequirePending();
            bool validCandidate = step.IsAuthoritative &&
                                  step.HasConsistentLandingEventIdentity &&
                                  (step.IsPreSwing || step.IsSwing) &&
                                  step.TimeToLandingSeconds > 0.000001f &&
                                  step.LandingEventIdentity != 0 &&
                                  step.LandingEventIdentity != Pending.LastLandingEventIdentity;
            if (!validCandidate)
            {
                m_Pending.InvalidateCurrentLanding();
                return;
            }

            if (m_Pending.NextSwingLanding.HasValue &&
                m_Pending.NextSwingLanding.LandingEventIdentity != step.LandingEventIdentity)
            {
                m_Pending.TrackedEventIdentity = 0;
                m_Pending.ClearNextSwingLanding();
            }

            m_Pending.TrackedEventIdentity = step.LandingEventIdentity;
            if (!m_Pending.NextSwingLanding.HasValue)
                m_Pending.State = CharacterFootLandingLifecycleState.Tracking;
            if (!diagnostics.Accepted ||
                diagnostics.LandingEventIdentity != step.LandingEventIdentity)
            {
                m_Pending.InvalidateCurrentLanding();
                return;
            }

            if (m_Pending.NextSwingLanding.HasValue)
            {
                Vector3 landingPoint = diagnostics.LandingPoint;
                m_Pending.NextSwingPredictionError = Vector3.Distance(
                    m_Pending.NextSwingReferencePoint,
                    landingPoint);
                m_Pending.NextSwingConstraintWeight = 1f;
                if (Vector3.Distance(
                        landingPoint,
                        m_Pending.NextSwingLanding.WorldPoint) <
                    settings.LandingUpdateDistance)
                {
                    m_Pending.State = CharacterFootLandingLifecycleState.Accepted;
                    return;
                }
                m_Pending.NextSwingLanding = CharacterFootLandingFact.Create(
                    in step,
                    in diagnostics);
                m_Pending.State = CharacterFootLandingLifecycleState.Accepted;
                return;
            }

            m_Pending.NextSwingLanding = CharacterFootLandingFact.Create(
                in step,
                in diagnostics);
            m_Pending.NextSwingReferencePoint = diagnostics.LandingPoint;
            m_Pending.NextSwingPredictionError = 0f;
            m_Pending.NextSwingConstraintWeight = 1f;
            m_Pending.State = CharacterFootLandingLifecycleState.Accepted;
        }

        internal void PromoteLanded(
            in AnimationBiomechanicalStepHeader step,
            bool completionCandidateReachable)
        {
            RequirePending();
            bool hasCurrentEvent = step.IsAuthoritative &&
                                   step.HasConsistentLandingEventIdentity &&
                                   step.LandingEventIdentity != 0;
            ulong currentEventIdentity = hasCurrentEvent
                ? step.LandingEventIdentity
                : 0;
            if (m_Pending.NextSwingLanding.HasValue)
            {
                ulong acceptedEventIdentity =
                    m_Pending.NextSwingLanding.LandingEventIdentity;
                bool completedInPlace = hasCurrentEvent &&
                                        currentEventIdentity == acceptedEventIdentity &&
                                        step.TimeToLandingSeconds <= 0.000001f;
                bool advancedToNextEvent = hasCurrentEvent &&
                                           m_Pending.ObservedCurrentEventIdentity == acceptedEventIdentity &&
                                           currentEventIdentity != acceptedEventIdentity;
                if (completedInPlace || advancedToNextEvent)
                {
                    m_Pending.LastLanding = completionCandidateReachable
                        ? m_Pending.NextSwingLanding
                        : default;
                    m_Pending.TrackedEventIdentity = 0;
                    m_Pending.ClearNextSwingLanding();
                }
            }
            else if (hasCurrentEvent &&
                     step.TimeToLandingSeconds <= 0.000001f &&
                     m_Pending.TrackedEventIdentity == currentEventIdentity)
            {
                m_Pending.LastLanding = default;
                m_Pending.TrackedEventIdentity = 0;
                m_Pending.State = CharacterFootLandingLifecycleState.Empty;
            }
            if (hasCurrentEvent)
                m_Pending.ObservedCurrentEventIdentity = currentEventIdentity;
        }

        internal void Seal()
        {
            RequirePending();
            m_Committed = m_Pending;
            ClearPending();
        }

        internal void Discard()
        {
            ClearPending();
        }

        internal void Reset()
        {
            m_Committed = default;
            ClearPending();
        }

        void ClearPending()
        {
            m_Pending = default;
            m_HasPending = false;
        }

        void RequirePending()
        {
            if (!m_HasPending)
            {
                throw new InvalidOperationException(
                    "Landing lifecycle has no pending frame.");
            }
        }
    }
}
