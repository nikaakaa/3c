using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using ThirdPersonGameplay.Tick;
using Unity.Profiling;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal enum CharacterBodyPresentationSourceMode : byte
    {
        CommittedStream = 1,
        SelectedStream = 2
    }

    internal enum CharacterBodyPresentationResetReason : byte
    {
        Initialization = 1,
        CommittedBranchReplacement = 2,
        SelectedStreamReset = 3
    }

    internal readonly struct CharacterBodyPresentationFrame
    {
        public CharacterBodyPresentationFrame(
            ulong previousTick,
            ulong currentTick,
            float sampleAlpha,
            float sampleAgeSeconds,
            CharacterBodyPresentationSourceMode sourceMode,
            CharacterVisualTrajectoryMode trajectoryMode,
            CharacterVisualTrajectorySample target,
            CharacterVisualTrajectoryResult visible,
            Vector3 sourceTranslationDelta,
            Vector3 visibleTranslationDelta,
            bool groundedBefore,
            bool groundedAfter,
            ulong resetSequence,
            CharacterBodyPresentationResetReason resetReason)
        {
            IsValid = currentTick != 0;
            PreviousTick = previousTick;
            CurrentTick = currentTick;
            SampleAlpha = Mathf.Clamp01(sampleAlpha);
            if (!float.IsFinite(sampleAgeSeconds) || sampleAgeSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(sampleAgeSeconds));
            SampleAgeSeconds = sampleAgeSeconds;
            SourceMode = sourceMode;
            TrajectoryMode = trajectoryMode;
            VisiblePosition = visible.Position;
            VisibleRotation = visible.Rotation;
            VisibleVelocity = visible.Velocity;
            VisibleYawVelocityDegreesPerSecond = visible.YawVelocityDegreesPerSecond;
            SourceTranslationDelta = sourceTranslationDelta;
            VisibleTranslationDelta = visibleTranslationDelta;
            GroundedBefore = groundedBefore;
            GroundedAfter = groundedAfter;
            TargetPosition = target.Position;
            TargetRotation = target.Rotation;
            TargetVelocity = target.LinearVelocity;
            TargetYawVelocityDegreesPerSecond = target.YawVelocityDegreesPerSecond;
            TargetGrounded = target.Grounded;
            PositionError = visible.PositionError.magnitude;
            RotationError = Mathf.Abs(visible.YawErrorDegrees);
            CorrectionPositionError = visible.PositionError;
            CorrectionPositionVelocity = visible.CorrectionVelocity;
            CorrectionYawVelocityDegreesPerSecond = visible.YawCorrectionVelocityDegreesPerSecond;
            CorrectionActive = visible.CorrectionActive;
            CorrectionClamped = visible.CorrectionClamped;
            CorrectionSettled = visible.Settled;
            ResetSequence = resetSequence;
            ResetReason = resetReason;
        }

        public bool IsValid { get; }
        public ulong PreviousTick { get; }
        public ulong CurrentTick { get; }
        public float SampleAlpha { get; }
        public float SampleAgeSeconds { get; }
        public ulong AnimationSampleTick => CurrentTick;
        public float AnimationSampleAlpha => SampleAlpha;
        public CharacterBodyPresentationSourceMode SourceMode { get; }
        public CharacterVisualTrajectoryMode TrajectoryMode { get; }
        public Vector3 VisiblePosition { get; }
        public Quaternion VisibleRotation { get; }
        public Vector3 VisibleVelocity { get; }
        public float VisibleYawVelocityDegreesPerSecond { get; }
        public Vector3 SourceTranslationDelta { get; }
        public Vector3 VisibleTranslationDelta { get; }
        public bool GroundedBefore { get; }
        public bool GroundedAfter { get; }
        public Vector3 TargetPosition { get; }
        public Quaternion TargetRotation { get; }
        public Vector3 TargetVelocity { get; }
        public float TargetYawVelocityDegreesPerSecond { get; }
        public bool TargetGrounded { get; }
        public float PositionError { get; }
        public float RotationError { get; }
        public Vector3 CorrectionPositionError { get; }
        public Vector3 CorrectionPositionVelocity { get; }
        public float CorrectionYawVelocityDegreesPerSecond { get; }
        public bool CorrectionActive { get; }
        public bool CorrectionClamped { get; }
        public bool CorrectionSettled { get; }
        public ulong ResetSequence { get; }
        public CharacterBodyPresentationResetReason ResetReason { get; }
    }

    internal sealed class CharacterBodyPresentationRuntime : IDisposable
    {
        static readonly ProfilerMarker BodyMarker = new ProfilerMarker("ThirdPerson.Presentation.Body");

        readonly ThirdPersonSimulation.ActorId m_ActorId;
        readonly int m_SimulationTickRate;
        readonly float m_TickDurationSeconds;
        readonly CharacterBodyPresentationSourceMode m_SourceMode;
        readonly CharacterVisualTrajectoryFollower m_Follower;
        readonly Transform m_VisualRoot;
        readonly Vector3 m_VisualBindPosition;
        readonly Quaternion m_VisualBindRotation;
        readonly CharacterPresentationBodyState m_InitialBody;
        readonly RuntimeDiagnosticsContext m_Diagnostics;
        readonly SortedDictionary<ulong, CharacterPresentationBodyState> m_CommittedBodies =
            new SortedDictionary<ulong, CharacterPresentationBodyState>();
        readonly Queue<CharacterPresentationBodyInterval> m_SelectedIntervals =
            new Queue<CharacterPresentationBodyInterval>();

        CharacterPresentationBodyInterval m_SelectedInterval;
        CharacterPresentationBodyState m_SelectedTailBody;
        ulong m_SelectedTailTick;
        ulong m_LatestTick;
        double m_CommittedPresentationTick;
        bool m_CommittedClockInitialized;
        bool m_CommittedClockNeedsReset = true;
        bool m_HasSelectedInterval;
        bool m_HasSelectedTail;
        float m_SelectedElapsedSeconds;
        ulong m_ResetSequence;
        CharacterBodyPresentationResetReason m_ResetReason;
        ulong m_BranchReplacementCount;
        CharacterBodyPresentationFrame m_LastPresentedFrame;
        bool m_Disposed;

        public CharacterBodyPresentationRuntime(
            ThirdPersonSimulation.ActorId actorId,
            int simulationTickRate,
            CharacterBodyPresentationSourceMode sourceMode,
            CharacterBodyPresentationSettings settings,
            Transform visualRoot,
            CharacterPresentationBodyState initialBody,
            RuntimeDiagnosticsContext diagnostics)
        {
            if (!actorId.IsValid || initialBody.ActorId != actorId)
                throw new ArgumentException("Presentation Body Runtime Actor identity is invalid.");
            if (simulationTickRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(simulationTickRate));
            if (sourceMode != CharacterBodyPresentationSourceMode.CommittedStream &&
                sourceMode != CharacterBodyPresentationSourceMode.SelectedStream)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceMode));
            }
            settings.RequireValid(nameof(CharacterBodyPresentationRuntime));
            m_ActorId = actorId;
            m_SimulationTickRate = simulationTickRate;
            m_TickDurationSeconds = 1f / simulationTickRate;
            m_SourceMode = sourceMode;
            m_Follower = new CharacterVisualTrajectoryFollower(settings);
            m_VisualRoot = visualRoot ? visualRoot : throw new ArgumentNullException(nameof(visualRoot));
            m_InitialBody = initialBody;
            m_Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            Quaternion inverse = Quaternion.Inverse(initialBody.Rotation);
            m_VisualBindPosition = inverse * (visualRoot.position - initialBody.Position);
            m_VisualBindRotation = inverse * visualRoot.rotation;
            InitializeState();
        }

        public CharacterBodyPresentationSourceMode SourceMode => m_SourceMode;
        internal Transform VisualRoot => m_VisualRoot;
        public ulong LatestTick => m_LatestTick;
        public ulong ResetSequence => m_ResetSequence;
        public ulong BranchReplacementCount => m_BranchReplacementCount;
        public float FollowerPositionCorrectionMeters => m_LastPresentedFrame.PositionError;
        public float FollowerYawCorrectionDegrees => m_LastPresentedFrame.RotationError;

        public void Capture(CharacterPresentationBodyInterval interval)
        {
            RequireAlive();
            if (interval.ActorId != m_ActorId)
                throw new InvalidOperationException("Presentation Body interval targets another Actor.");
            if (m_SourceMode == CharacterBodyPresentationSourceMode.CommittedStream)
                CaptureCommitted(interval);
            else
                CaptureSelected(interval);
        }

        public void CaptureTransaction(IReadOnlyList<CharacterPresentationBodyInterval> intervals)
        {
            RequireAlive();
            if (intervals == null || intervals.Count == 0)
                throw new ArgumentException("Presentation Body transaction requires at least one interval.", nameof(intervals));
            if (m_SourceMode != CharacterBodyPresentationSourceMode.CommittedStream)
            {
                throw new InvalidOperationException(
                    "Presentation Body transaction is only valid for a committed simulation stream.");
            }
            ValidateCommittedTransaction(intervals);
            bool replacesBranch = ReplacesCommittedBranch(intervals[0]);
            if (!replacesBranch && intervals[0].PreviousTick != m_LatestTick)
            {
                throw new InvalidOperationException(
                    $"Committed Presentation Body transaction starts at Tick '{intervals[0].PreviousTick}' but latest Tick is '{m_LatestTick}'.");
            }
            if (replacesBranch && m_CommittedClockInitialized &&
                intervals[intervals.Count - 1].CurrentTick < m_CommittedPresentationTick)
            {
                throw new InvalidOperationException(
                    "Committed Presentation branch replacement does not cover the current Presentation cursor.");
            }
            if (replacesBranch)
            {
                m_BranchReplacementCount = checked(m_BranchReplacementCount + 1);
                RemoveCommittedBranchFrom(intervals[0].PreviousTick);
            }
            for (int i = 0; i < intervals.Count; i++)
                StoreCommitted(intervals[i]);
            if (replacesBranch)
                RetargetCommittedBranch();
        }

        public CharacterBodyPresentationFrame Present(GameplayPresentationFrameContext context)
        {
            RequireAlive();
            if (m_LatestTick == 0)
                return default;
            using (BodyMarker.Auto())
            {
                CharacterBodyPresentationFrame frame = m_SourceMode == CharacterBodyPresentationSourceMode.CommittedStream
                    ? PresentCommitted(context)
                    : PresentSelected(context);
                m_LastPresentedFrame = frame;
                ApplyVisualRoot(frame);
                PublishDiagnostics(frame, context.ScaledDeltaSeconds);
                return frame;
            }
        }

        public void Reset()
        {
            if (m_Disposed)
                return;
            InitializeState();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            InitializeState();
            m_Follower.Clear();
            m_Disposed = true;
        }

        void CaptureCommitted(CharacterPresentationBodyInterval interval)
        {
            ValidateCommittedInterval(interval);
            bool replacesBranch = ReplacesCommittedBranch(interval);
            if (!replacesBranch && interval.PreviousTick != m_LatestTick)
            {
                throw new InvalidOperationException(
                    $"Committed Presentation Body interval starts at Tick '{interval.PreviousTick}' but latest Tick is '{m_LatestTick}'.");
            }
            if (replacesBranch && m_CommittedClockInitialized && interval.CurrentTick < m_CommittedPresentationTick)
            {
                throw new InvalidOperationException(
                    "Committed Presentation branch replacement does not cover the current Presentation cursor.");
            }
            if (replacesBranch)
            {
                m_BranchReplacementCount = checked(m_BranchReplacementCount + 1);
                RemoveCommittedBranchFrom(interval.PreviousTick);
            }
            StoreCommitted(interval);
            if (replacesBranch)
                RetargetCommittedBranch();
        }

        void ValidateCommittedTransaction(IReadOnlyList<CharacterPresentationBodyInterval> intervals)
        {
            for (int i = 0; i < intervals.Count; i++)
            {
                CharacterPresentationBodyInterval interval = intervals[i];
                ValidateCommittedInterval(interval);
                if (i == 0)
                    continue;
                CharacterPresentationBodyInterval previous = intervals[i - 1];
                if (interval.PreviousTick != previous.CurrentTick ||
                    !HasSameKinematicState(interval.PreviousBody, previous.CurrentBody))
                {
                    throw new InvalidOperationException(
                        $"Committed Presentation Body transaction is discontinuous at Tick '{interval.CurrentTick}'.");
                }
            }
        }

        void ValidateCommittedInterval(CharacterPresentationBodyInterval interval)
        {
            if (interval.ActorId != m_ActorId)
                throw new InvalidOperationException("Presentation Body interval targets another Actor.");
            if (interval.UpdateKind == CharacterPresentationBodyStreamUpdateKind.Reset)
            {
                throw new InvalidOperationException(
                    "Committed Presentation Body stream does not accept selected-stream Reset updates.");
            }
        }

        bool ReplacesCommittedBranch(CharacterPresentationBodyInterval interval)
        {
            bool replacesLatestPrevious = interval.PreviousTick == m_LatestTick &&
                m_CommittedBodies.TryGetValue(interval.PreviousTick, out CharacterPresentationBodyState existingPrevious) &&
                !HasSameKinematicState(existingPrevious, interval.PreviousBody);
            return m_LatestTick != 0 &&
                (interval.PreviousTick < m_LatestTick || interval.CurrentTick <= m_LatestTick || replacesLatestPrevious);
        }

        void RetargetCommittedBranch()
        {
            AdvanceReset(CharacterBodyPresentationResetReason.CommittedBranchReplacement);
            if (!m_CommittedClockInitialized)
                return;
            if (!TrySampleCommittedTarget(m_CommittedPresentationTick, out CharacterBodyTargetFrame target))
            {
                throw new InvalidOperationException(
                    "Committed Presentation branch replacement cannot sample the current Presentation cursor.");
            }
            m_Follower.Retarget(target.Sample);
        }

        void RemoveCommittedBranchFrom(ulong firstTick)
        {
            var obsolete = new List<ulong>();
            foreach (ulong tick in m_CommittedBodies.Keys)
            {
                if (tick >= firstTick)
                    obsolete.Add(tick);
            }
            for (int i = 0; i < obsolete.Count; i++)
                m_CommittedBodies.Remove(obsolete[i]);
        }

        void StoreCommitted(CharacterPresentationBodyInterval interval)
        {
            m_CommittedBodies[interval.PreviousTick] = interval.PreviousBody;
            m_CommittedBodies[interval.CurrentTick] = interval.CurrentBody;
            m_LatestTick = interval.CurrentTick;
        }

        void CaptureSelected(CharacterPresentationBodyInterval interval)
        {
            if (interval.UpdateKind == CharacterPresentationBodyStreamUpdateKind.Reset)
            {
                ResetSelectedSource(interval.PreviousBody, interval.PreviousTick);
            }
            else
            {
                if (!m_HasSelectedTail ||
                    interval.PreviousTick != m_SelectedTailTick ||
                    !HasSameKinematicState(interval.PreviousBody, m_SelectedTailBody))
                {
                    throw new InvalidOperationException(
                        "Selected Presentation Body interval is discontinuous without an explicit stream Reset.");
                }
                if (interval.CurrentTick <= m_LatestTick)
                {
                    throw new InvalidOperationException(
                        "Selected Presentation Body Tick duplicated or regressed without an explicit stream Reset.");
                }
            }

            if (m_HasSelectedInterval)
                m_SelectedIntervals.Enqueue(interval);
            else
            {
                m_SelectedInterval = interval;
                m_SelectedElapsedSeconds = 0f;
                m_HasSelectedInterval = true;
            }
            m_SelectedTailTick = interval.CurrentTick;
            m_SelectedTailBody = interval.CurrentBody;
            m_HasSelectedTail = true;
            m_LatestTick = interval.CurrentTick;
        }

        CharacterBodyPresentationFrame PresentCommitted(GameplayPresentationFrameContext context)
        {
            if (m_CommittedBodies.Count == 0)
                throw new InvalidOperationException("Committed Presentation Body stream has no samples.");
            ulong firstTick = FirstCommittedTick();
            ulong latestTick = LastCommittedTick();
            if (!m_CommittedClockInitialized || m_CommittedClockNeedsReset)
            {
                double initial = latestTick > firstTick
                    ? latestTick - 1d + Mathf.Clamp01(context.InterpolationAlpha)
                    : latestTick;
                m_CommittedPresentationTick = Math.Max(firstTick, Math.Min(latestTick, initial));
                m_CommittedClockInitialized = true;
                m_CommittedClockNeedsReset = false;
            }
            else
            {
                if (latestTick < m_CommittedPresentationTick)
                    throw new InvalidOperationException("Committed Presentation cursor cannot move backward.");
                m_CommittedPresentationTick = Math.Min(
                    latestTick,
                    m_CommittedPresentationTick + Math.Max(0f, context.ScaledDeltaSeconds) * m_SimulationTickRate);
                if (m_CommittedPresentationTick < firstTick)
                    m_CommittedPresentationTick = firstTick;
            }

            if (!TrySampleCommittedTarget(m_CommittedPresentationTick, out CharacterBodyTargetFrame target))
                throw new InvalidOperationException("Committed Presentation Body target cannot be sampled.");
            CharacterVisualTrajectoryResult visible = m_Follower.Evaluate(
                target.Sample,
                context.ScaledDeltaSeconds);
            TrimCommittedBodies(target.PreviousTick);
            return BuildFrame(target, visible);
        }

        CharacterBodyPresentationFrame PresentSelected(GameplayPresentationFrameContext context)
        {
            if (!m_HasSelectedInterval)
                return default;
            float deltaSeconds = Math.Max(0f, context.ScaledDeltaSeconds);
            float intervalDuration = SelectedIntervalDuration(m_SelectedInterval);
            float remainingSeconds = deltaSeconds;
            while (remainingSeconds > 0f)
            {
                float intervalRemaining = Math.Max(0f, intervalDuration - m_SelectedElapsedSeconds);
                float consumed = Math.Min(intervalRemaining, remainingSeconds);
                m_SelectedElapsedSeconds += consumed;
                remainingSeconds -= consumed;
                if (m_SelectedElapsedSeconds < intervalDuration || m_SelectedIntervals.Count == 0)
                    break;
                m_SelectedInterval = m_SelectedIntervals.Dequeue();
                m_SelectedElapsedSeconds = 0f;
                intervalDuration = SelectedIntervalDuration(m_SelectedInterval);
            }
            float alpha = intervalDuration <= 0f
                ? 1f
                : Mathf.Clamp01(m_SelectedElapsedSeconds / intervalDuration);
            CharacterBodyTargetFrame target = BuildTarget(
                m_SelectedInterval.PreviousTick,
                m_SelectedInterval.PreviousBody,
                m_SelectedInterval.CurrentTick,
                m_SelectedInterval.CurrentBody,
                alpha);
            CharacterVisualTrajectoryResult visible = m_Follower.Evaluate(target.Sample, deltaSeconds);
            return BuildFrame(target, visible);
        }

        bool TrySampleCommittedTarget(double sampleTick, out CharacterBodyTargetFrame target)
        {
            target = default;
            if (!m_CommittedClockInitialized || m_CommittedBodies.Count == 0)
                return false;
            ulong firstTick = FirstCommittedTick();
            ulong latestTick = LastCommittedTick();
            if (sampleTick < firstTick || sampleTick > latestTick)
                return false;
            ulong previousTick = firstTick;
            CharacterPresentationBodyState previousBody = m_CommittedBodies[firstTick];
            foreach (KeyValuePair<ulong, CharacterPresentationBodyState> pair in m_CommittedBodies)
            {
                if (pair.Key <= sampleTick)
                {
                    previousTick = pair.Key;
                    previousBody = pair.Value;
                    continue;
                }
                float alpha = Mathf.Clamp01((float)((sampleTick - previousTick) / (pair.Key - previousTick)));
                target = BuildTarget(previousTick, previousBody, pair.Key, pair.Value, alpha);
                return true;
            }
            target = BuildTarget(previousTick, previousBody, previousTick, previousBody, 1f);
            return true;
        }

        CharacterBodyTargetFrame BuildTarget(
            ulong previousTick,
            CharacterPresentationBodyState previousBody,
            ulong currentTick,
            CharacterPresentationBodyState currentBody,
            float alpha)
        {
            float clampedAlpha = Mathf.Clamp01(alpha);
            float yawVelocity = currentTick == previousTick
                ? 0f
                : Mathf.DeltaAngle(previousBody.Rotation.eulerAngles.y, currentBody.Rotation.eulerAngles.y) /
                  ((currentTick - previousTick) * m_TickDurationSeconds);
            var sample = new CharacterVisualTrajectorySample(
                Vector3.Lerp(previousBody.Position, currentBody.Position, clampedAlpha),
                Quaternion.Slerp(previousBody.Rotation, currentBody.Rotation, clampedAlpha),
                Vector3.Lerp(previousBody.LinearVelocity, currentBody.LinearVelocity, clampedAlpha),
                yawVelocity,
                clampedAlpha < 1f
                    ? previousBody.Grounded && currentBody.Grounded
                    : currentBody.Grounded);
            return new CharacterBodyTargetFrame(
                previousTick,
                currentTick,
                clampedAlpha,
                sample,
                currentBody.Position - previousBody.Position,
                previousBody.Grounded,
                currentBody.Grounded);
        }

        CharacterBodyPresentationFrame BuildFrame(
            CharacterBodyTargetFrame target,
            CharacterVisualTrajectoryResult visible)
        {
            Vector3 visibleTranslationDelta = m_LastPresentedFrame.IsValid &&
                                              m_LastPresentedFrame.ResetSequence == m_ResetSequence
                ? visible.Position - m_LastPresentedFrame.VisiblePosition
                : Vector3.zero;
            double sampleTick = target.PreviousTick +
                                (target.CurrentTick - target.PreviousTick) * (double)target.SampleAlpha;
            float sampleAgeSeconds = m_SourceMode == CharacterBodyPresentationSourceMode.SelectedStream
                ? (float)(Math.Max(0d, m_LatestTick - sampleTick) * m_TickDurationSeconds)
                : 0f;
            return new CharacterBodyPresentationFrame(
                target.PreviousTick,
                target.CurrentTick,
                target.SampleAlpha,
                sampleAgeSeconds,
                m_SourceMode,
                m_Follower.Mode,
                target.Sample,
                visible,
                target.SourceTranslationDelta,
                visibleTranslationDelta,
                target.GroundedBefore,
                target.GroundedAfter,
                m_ResetSequence,
                m_ResetReason);
        }

        void ApplyVisualRoot(CharacterBodyPresentationFrame frame)
        {
            if (!frame.IsValid)
                return;
            m_VisualRoot.SetPositionAndRotation(
                frame.VisiblePosition + frame.VisibleRotation * m_VisualBindPosition,
                frame.VisibleRotation * m_VisualBindRotation);
        }

        void PublishDiagnostics(CharacterBodyPresentationFrame frame, float presentationDeltaSeconds)
        {
            if (!frame.IsValid ||
                !m_Diagnostics.ShouldPublish(RuntimeTraceChannel.Animation, RuntimeTraceEventKind.PresentationInterpolated))
            {
                return;
            }
            m_Diagnostics.Publish(
                RuntimeTraceChannel.Animation,
                RuntimeTraceDomain.Presentation,
                RuntimeTraceEventKind.PresentationInterpolated,
                RuntimeSourceElementHandle.Invalid,
                RuntimeInstanceKey.Character(m_Diagnostics.CharacterRuntimeId),
                new RuntimeTracePayload
                {
                    Status = frame.CorrectionActive ? "Correcting" : "Settled",
                    Time = frame.SampleAlpha,
                    SecondaryTime = presentationDeltaSeconds,
                    Detail = $"{frame.PreviousTick}->{frame.CurrentTick};source={frame.SourceMode};trajectory={frame.TrajectoryMode};sampleAge={frame.SampleAgeSeconds:0.####};target={frame.TargetPosition};targetYaw={frame.TargetRotation.eulerAngles.y:0.###};targetVelocity={frame.TargetVelocity};targetYawVelocity={frame.TargetYawVelocityDegreesPerSecond:0.###};grounded={frame.TargetGrounded};groundedInterval={frame.GroundedBefore}->{frame.GroundedAfter};sourceDelta={frame.SourceTranslationDelta};visibleDelta={frame.VisibleTranslationDelta};visual={frame.VisiblePosition};visualYaw={frame.VisibleRotation.eulerAngles.y:0.###};visualVelocity={frame.VisibleVelocity};visualYawVelocity={frame.VisibleYawVelocityDegreesPerSecond:0.###};positionError={frame.PositionError:0.####};yawError={frame.RotationError:0.###};correctionVelocity={frame.CorrectionPositionVelocity};yawCorrectionVelocity={frame.CorrectionYawVelocityDegreesPerSecond:0.###};active={frame.CorrectionActive};clamped={frame.CorrectionClamped};settled={frame.CorrectionSettled};branchRevision={frame.ResetSequence};resetReason={frame.ResetReason}",
                    Value = DebugValueSnapshot.Capture(frame.VisiblePosition)
                });
        }

        void InitializeState()
        {
            m_CommittedBodies.Clear();
            m_CommittedPresentationTick = 0d;
            m_CommittedClockInitialized = false;
            m_CommittedClockNeedsReset = true;
            m_SelectedInterval = default;
            m_SelectedIntervals.Clear();
            m_HasSelectedInterval = false;
            m_SelectedElapsedSeconds = 0f;
            m_SelectedTailTick = 0;
            m_SelectedTailBody = m_InitialBody;
            m_HasSelectedTail = true;
            m_LatestTick = 0;
            m_ResetSequence = 0;
            m_ResetReason = CharacterBodyPresentationResetReason.Initialization;
            m_BranchReplacementCount = 0;
            m_LastPresentedFrame = default;
            m_Follower.Reset(ToAnchorSample(m_InitialBody));
            if (m_SourceMode == CharacterBodyPresentationSourceMode.CommittedStream)
                m_CommittedBodies.Add(0, m_InitialBody);
            AdvanceReset(CharacterBodyPresentationResetReason.Initialization);
        }

        void ResetSelectedSource(CharacterPresentationBodyState anchor, ulong anchorTick)
        {
            if (anchor.ActorId != m_ActorId)
                throw new InvalidOperationException("Selected Presentation reset anchor targets another Actor.");
            m_SelectedInterval = default;
            m_SelectedIntervals.Clear();
            m_HasSelectedInterval = false;
            m_SelectedElapsedSeconds = 0f;
            m_SelectedTailTick = anchorTick;
            m_SelectedTailBody = anchor;
            m_HasSelectedTail = true;
            m_LatestTick = anchorTick;
            m_Follower.Retarget(ToAnchorSample(anchor));
            AdvanceReset(CharacterBodyPresentationResetReason.SelectedStreamReset);
        }

        float SelectedIntervalDuration(CharacterPresentationBodyInterval interval)
        {
            return Math.Max(
                m_TickDurationSeconds,
                (interval.CurrentTick - interval.PreviousTick) * m_TickDurationSeconds);
        }

        void AdvanceReset(CharacterBodyPresentationResetReason reason)
        {
            m_ResetSequence++;
            if (m_ResetSequence == 0)
                m_ResetSequence++;
            m_ResetReason = reason;
        }

        ulong FirstCommittedTick()
        {
            foreach (ulong tick in m_CommittedBodies.Keys)
                return tick;
            throw new InvalidOperationException("Committed Presentation Body history is empty.");
        }

        ulong LastCommittedTick()
        {
            ulong tick = 0;
            foreach (ulong candidate in m_CommittedBodies.Keys)
                tick = candidate;
            return tick;
        }

        void TrimCommittedBodies(ulong retainTick)
        {
            while (m_CommittedBodies.Count > 2 && FirstCommittedTick() < retainTick)
                m_CommittedBodies.Remove(FirstCommittedTick());
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterBodyPresentationRuntime));
        }

        static CharacterVisualTrajectorySample ToAnchorSample(CharacterPresentationBodyState body)
        {
            return new CharacterVisualTrajectorySample(
                body.Position,
                body.Rotation,
                body.LinearVelocity,
                0f,
                body.Grounded);
        }

        static bool HasSameKinematicState(
            CharacterPresentationBodyState left,
            CharacterPresentationBodyState right)
        {
            return left.Position.Equals(right.Position) &&
                   left.Rotation.Equals(right.Rotation) &&
                   left.LinearVelocity.Equals(right.LinearVelocity) &&
                   left.Grounded == right.Grounded;
        }

        readonly struct CharacterBodyTargetFrame
        {
            public CharacterBodyTargetFrame(
                ulong previousTick,
                ulong currentTick,
                float sampleAlpha,
                CharacterVisualTrajectorySample sample,
                Vector3 sourceTranslationDelta,
                bool groundedBefore,
                bool groundedAfter)
            {
                PreviousTick = previousTick;
                CurrentTick = currentTick;
                SampleAlpha = sampleAlpha;
                Sample = sample;
                SourceTranslationDelta = sourceTranslationDelta;
                GroundedBefore = groundedBefore;
                GroundedAfter = groundedAfter;
            }

            public ulong PreviousTick { get; }
            public ulong CurrentTick { get; }
            public float SampleAlpha { get; }
            public CharacterVisualTrajectorySample Sample { get; }
            public Vector3 SourceTranslationDelta { get; }
            public bool GroundedBefore { get; }
            public bool GroundedAfter { get; }
        }
    }
}
