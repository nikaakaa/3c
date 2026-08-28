using System;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal enum CharacterFootPredictionMotionRejectReason : byte
    {
        None = 0,
        TimelineUnavailable = 1,
        SourceUnavailable = 2,
        InvalidInput = 3
    }

    internal enum CharacterFootPredictionMotionResetReason : byte
    {
        None = 0,
        Initialization = 1,
        BodyReset = 2,
        TimelineGenerationChanged = 3,
        PredictionSourceChanged = 4
    }

    internal struct CharacterFootPredictionMotionState
    {
        internal bool HasValue;
        internal Vector2 StableCurrentVelocity;
        internal Vector2 StableContinuationVelocity;
        internal ulong TimelineGeneration;
        internal ulong BodyResetSequence;
        internal FixedString128Bytes PredictionSourceIdentity;
        internal ulong Revision;

        internal void Clear() => this = default;
    }

    internal readonly struct CharacterFootPredictionMotionResult
    {
        internal CharacterFootPredictionMotionResult(
            CharacterFootPredictionMotionRejectReason rejectReason,
            CharacterFootPredictionMotionResetReason resetReason,
            string predictionSourceIdentity,
            Vector2 rawCurrentVelocity,
            Vector2 rawContinuationVelocity,
            Vector2 previousStableCurrentVelocity,
            Vector2 previousStableContinuationVelocity,
            Vector2 stableCurrentVelocity,
            Vector2 stableContinuationVelocity,
            Vector2 currentVelocityDelta,
            Vector2 continuationVelocityDelta,
            float responseAlpha,
            float deltaThreshold,
            float smoothSpeed,
            float maximumSpeed,
            bool currentResponseApplied,
            bool continuationResponseApplied,
            bool currentMaximumSpeedClamped,
            bool continuationMaximumSpeedClamped,
            ulong revision)
        {
            RejectReason = rejectReason;
            ResetReason = resetReason;
            PredictionSourceIdentity = predictionSourceIdentity ?? string.Empty;
            RawCurrentVelocity = rawCurrentVelocity;
            RawContinuationVelocity = rawContinuationVelocity;
            PreviousStableCurrentVelocity = previousStableCurrentVelocity;
            PreviousStableContinuationVelocity = previousStableContinuationVelocity;
            StableCurrentVelocity = stableCurrentVelocity;
            StableContinuationVelocity = stableContinuationVelocity;
            CurrentVelocityDelta = currentVelocityDelta;
            ContinuationVelocityDelta = continuationVelocityDelta;
            ResponseAlpha = responseAlpha;
            DeltaThreshold = deltaThreshold;
            SmoothSpeed = smoothSpeed;
            MaximumSpeed = maximumSpeed;
            CurrentResponseApplied = currentResponseApplied;
            ContinuationResponseApplied = continuationResponseApplied;
            CurrentMaximumSpeedClamped = currentMaximumSpeedClamped;
            ContinuationMaximumSpeedClamped = continuationMaximumSpeedClamped;
            Revision = revision;
        }

        internal bool IsValid =>
            RejectReason == CharacterFootPredictionMotionRejectReason.None;
        internal CharacterFootPredictionMotionRejectReason RejectReason { get; }
        internal CharacterFootPredictionMotionResetReason ResetReason { get; }
        internal string PredictionSourceIdentity { get; }
        internal Vector2 RawCurrentVelocity { get; }
        internal Vector2 RawContinuationVelocity { get; }
        internal Vector2 PreviousStableCurrentVelocity { get; }
        internal Vector2 PreviousStableContinuationVelocity { get; }
        internal Vector2 StableCurrentVelocity { get; }
        internal Vector2 StableContinuationVelocity { get; }
        internal Vector2 CurrentVelocityDelta { get; }
        internal Vector2 ContinuationVelocityDelta { get; }
        internal float ResponseAlpha { get; }
        internal float DeltaThreshold { get; }
        internal float SmoothSpeed { get; }
        internal float MaximumSpeed { get; }
        internal bool CurrentResponseApplied { get; }
        internal bool ContinuationResponseApplied { get; }
        internal bool CurrentMaximumSpeedClamped { get; }
        internal bool ContinuationMaximumSpeedClamped { get; }
        internal ulong Revision { get; }
    }

    internal static class CharacterFootPredictionMotionRuntime
    {
        const float ChangeEpsilon = 0.000001f;

        internal static CharacterFootPredictionMotionResult Evaluate(
            ref CharacterFootPredictionMotionState state,
            bool timelineAvailable,
            ulong timelineGeneration,
            ulong bodyResetSequence,
            string predictionSourceIdentity,
            Vector2 rawCurrentVelocity,
            Vector2 rawContinuationVelocity,
            float deltaSeconds,
            in CharacterFootLandingPredictionSettings settings)
        {
            if (!timelineAvailable || timelineGeneration == 0)
            {
                return Rejected(
                    CharacterFootPredictionMotionRejectReason.TimelineUnavailable,
                    predictionSourceIdentity,
                    rawCurrentVelocity,
                    rawContinuationVelocity,
                    in settings);
            }
            if (string.IsNullOrWhiteSpace(predictionSourceIdentity))
            {
                return Rejected(
                    CharacterFootPredictionMotionRejectReason.SourceUnavailable,
                    predictionSourceIdentity,
                    rawCurrentVelocity,
                    rawContinuationVelocity,
                    in settings);
            }
            if (!Finite(rawCurrentVelocity) ||
                !Finite(rawContinuationVelocity) ||
                !float.IsFinite(deltaSeconds) || deltaSeconds <= 0f)
            {
                return Rejected(
                    CharacterFootPredictionMotionRejectReason.InvalidInput,
                    predictionSourceIdentity,
                    rawCurrentVelocity,
                    rawContinuationVelocity,
                    in settings);
            }

            var sourceIdentity = new FixedString128Bytes(predictionSourceIdentity);
            CharacterFootPredictionMotionResetReason resetReason = ResolveResetReason(
                in state,
                timelineGeneration,
                bodyResetSequence,
                in sourceIdentity);
            Vector2 previousCurrent = state.StableCurrentVelocity;
            Vector2 previousContinuation = state.StableContinuationVelocity;
            float responseAlpha = Mathf.Clamp01(
                settings.PredictionVelocitySmoothSpeed * deltaSeconds);
            Vector2 currentDelta = rawCurrentVelocity - previousCurrent;
            Vector2 continuationDelta =
                rawContinuationVelocity - previousContinuation;
            bool currentResponseApplied = false;
            bool continuationResponseApplied = false;
            bool currentMaximumSpeedClamped;
            bool continuationMaximumSpeedClamped;
            Vector2 stableCurrent;
            Vector2 stableContinuation;
            if (resetReason != CharacterFootPredictionMotionResetReason.None)
            {
                stableCurrent = ClampMagnitude(
                    rawCurrentVelocity,
                    settings.PredictionMaximumSpeed,
                    out currentMaximumSpeedClamped);
                stableContinuation = ClampMagnitude(
                    rawContinuationVelocity,
                    settings.PredictionMaximumSpeed,
                    out continuationMaximumSpeedClamped);
            }
            else
            {
                Vector2 currentCandidate = previousCurrent;
                if (currentDelta.magnitude >
                    settings.PredictionVelocityDeltaThreshold)
                {
                    currentCandidate += currentDelta * responseAlpha;
                    currentResponseApplied = true;
                }
                Vector2 continuationCandidate = previousContinuation;
                if (continuationDelta.magnitude >
                    settings.PredictionVelocityDeltaThreshold)
                {
                    continuationCandidate += continuationDelta * responseAlpha;
                    continuationResponseApplied = true;
                }
                stableCurrent = ClampMagnitude(
                    currentCandidate,
                    settings.PredictionMaximumSpeed,
                    out currentMaximumSpeedClamped);
                stableContinuation = ClampMagnitude(
                    continuationCandidate,
                    settings.PredictionMaximumSpeed,
                    out continuationMaximumSpeedClamped);
            }

            bool changed =
                resetReason != CharacterFootPredictionMotionResetReason.None ||
                (stableCurrent - previousCurrent).sqrMagnitude >
                ChangeEpsilon * ChangeEpsilon ||
                (stableContinuation - previousContinuation).sqrMagnitude >
                ChangeEpsilon * ChangeEpsilon;
            ulong revision = state.Revision;
            if (changed)
                revision = revision == ulong.MaxValue ? 1 : revision + 1;
            state.HasValue = true;
            state.StableCurrentVelocity = stableCurrent;
            state.StableContinuationVelocity = stableContinuation;
            state.TimelineGeneration = timelineGeneration;
            state.BodyResetSequence = bodyResetSequence;
            state.PredictionSourceIdentity = sourceIdentity;
            state.Revision = revision;
            return new CharacterFootPredictionMotionResult(
                CharacterFootPredictionMotionRejectReason.None,
                resetReason,
                predictionSourceIdentity,
                rawCurrentVelocity,
                rawContinuationVelocity,
                previousCurrent,
                previousContinuation,
                stableCurrent,
                stableContinuation,
                currentDelta,
                continuationDelta,
                responseAlpha,
                settings.PredictionVelocityDeltaThreshold,
                settings.PredictionVelocitySmoothSpeed,
                settings.PredictionMaximumSpeed,
                currentResponseApplied,
                continuationResponseApplied,
                currentMaximumSpeedClamped,
                continuationMaximumSpeedClamped,
                revision);
        }

        static CharacterFootPredictionMotionResetReason ResolveResetReason(
            in CharacterFootPredictionMotionState state,
            ulong timelineGeneration,
            ulong bodyResetSequence,
            in FixedString128Bytes predictionSourceIdentity)
        {
            if (!state.HasValue)
                return CharacterFootPredictionMotionResetReason.Initialization;
            if (state.BodyResetSequence != bodyResetSequence)
                return CharacterFootPredictionMotionResetReason.BodyReset;
            if (state.TimelineGeneration != timelineGeneration)
            {
                return CharacterFootPredictionMotionResetReason
                    .TimelineGenerationChanged;
            }
            if (!state.PredictionSourceIdentity.Equals(predictionSourceIdentity))
            {
                return CharacterFootPredictionMotionResetReason
                    .PredictionSourceChanged;
            }
            return CharacterFootPredictionMotionResetReason.None;
        }

        static CharacterFootPredictionMotionResult Rejected(
            CharacterFootPredictionMotionRejectReason reason,
            string predictionSourceIdentity,
            Vector2 rawCurrentVelocity,
            Vector2 rawContinuationVelocity,
            in CharacterFootLandingPredictionSettings settings) =>
            new CharacterFootPredictionMotionResult(
                reason,
                CharacterFootPredictionMotionResetReason.None,
                predictionSourceIdentity,
                rawCurrentVelocity,
                rawContinuationVelocity,
                default,
                default,
                default,
                default,
                default,
                default,
                0f,
                settings.PredictionVelocityDeltaThreshold,
                settings.PredictionVelocitySmoothSpeed,
                settings.PredictionMaximumSpeed,
                false,
                false,
                false,
                false,
                0);

        static Vector2 ClampMagnitude(
            Vector2 value,
            float maximum,
            out bool clamped)
        {
            float magnitude = value.magnitude;
            clamped = magnitude > maximum;
            return clamped ? value * (maximum / magnitude) : value;
        }

        static bool Finite(Vector2 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y);
    }
}
