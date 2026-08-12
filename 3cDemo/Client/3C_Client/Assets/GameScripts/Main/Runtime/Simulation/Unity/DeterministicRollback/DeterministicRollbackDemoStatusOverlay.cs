using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonSimulation.DeterministicRollback;
using ThirdPersonGameplay.Tick;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.DeterministicRollback
{
    [DisallowMultipleComponent]
    public sealed class DeterministicRollbackDemoStatusOverlay : MonoBehaviour
    {
        [SerializeField] DeterministicRollbackCharacterHost[] m_Actors;
        readonly Guid m_PoseProbeOwnerId = Guid.NewGuid();
        readonly List<AnimationPresentationRuntimeTarget> m_PoseProbeTargets =
            new List<AnimationPresentationRuntimeTarget>();
        readonly Dictionary<Guid, PoseProbeState> m_PoseProbeStates =
            new Dictionary<Guid, PoseProbeState>();
        float m_SmoothedFrameDeltaSeconds;

#if UNITY_EDITOR
        public void SetActors(params DeterministicRollbackCharacterHost[] actors)
        {
            m_Actors = actors == null
                ? throw new System.ArgumentNullException(nameof(actors))
                : (DeterministicRollbackCharacterHost[])actors.Clone();
        }
#endif

        void Update()
        {
            float deltaSeconds = Time.unscaledDeltaTime;
            if (deltaSeconds <= 0f)
                return;
            float blend = 1f - Mathf.Exp(-4f * deltaSeconds);
            m_SmoothedFrameDeltaSeconds = m_SmoothedFrameDeltaSeconds <= 0f
                ? deltaSeconds
                : Mathf.Lerp(m_SmoothedFrameDeltaSeconds, deltaSeconds, blend);
            UpdatePoseProbe();
        }

        void OnGUI()
        {
            DeterministicRollbackCharacterHost local = FindLocalActor();
            GUILayout.BeginArea(new Rect(16f, 16f, 980f, 520f), GUI.skin.box);
            GUILayout.Label($"Model: {DeterministicRollbackModelIdentity.ModelId}");
            if (!local)
            {
                GUILayout.Label("Local peer actor is not configured.");
                GUILayout.EndArea();
                return;
            }
            GUILayout.Label($"Local actor: {local.ActorId} | Session: {local.SessionHost.LifecycleState}");
            if (GameplayTickSystem.IsInitialized)
            {
                GameplayTickSystem tick = GameplayTickSystem.Current;
                float frameRate = m_SmoothedFrameDeltaSeconds > 0f ? 1f / m_SmoothedFrameDeltaSeconds : 0f;
                GUILayout.Label(
                    $"Frame: render={tick.RenderFrame} logic={tick.LocalLogicTick} dropped={tick.DroppedLocalLogicTicks} " +
                    $"fps={frameRate:F1} frameMs={m_SmoothedFrameDeltaSeconds * 1000f:F2} alpha={tick.InterpolationAlpha:F3} " +
                    $"vSync={QualitySettings.vSyncCount} targetFps={Application.targetFrameRate}");
            }
            if (local.SessionHost.Failure != null)
                GUILayout.Label($"Failure: {local.SessionHost.Failure.Code} | {local.SessionHost.Failure.Message}");
            if (local.TryGetRuntimeDiagnostics(out RollbackRuntimeDiagnosticsSnapshot value))
            {
                GUILayout.Label($"Tick predicted/completed/confirmed: {value.CompletedTick + 1}/{value.CompletedTick}/{value.ConfirmedTick}");
                GUILayout.Label($"Timing: offensiveDelay={value.OffensiveRequestDelayTicks} confirmationDelay={value.ConfirmationDelayTicks} predictionLead={value.PredictionLeadTicks}/{value.MaximumPredictionLeadTicks} pacedNoStep={value.PacedNoStepCount}");
                GUILayout.Label($"Input history: {value.InputHistoryCount} [{value.InputHistoryFloor}, {value.InputHistoryCeiling}] late={value.LateInputCount} lastLate={value.LastLateInputTick}");
                GUILayout.Label($"Offensive pending: count={value.InputSource.Local.PendingOffensiveRequestCount} oldestCapture={value.InputSource.Local.OldestCaptureTick} eligible={value.InputSource.Local.OldestEligibleTick}");
                GUILayout.Label($"Relay arrival: count={value.InputSource.RelayedArrivalCount} delta={value.InputSource.LastRelayedArrivalDeltaTicks} lead={value.InputSource.RelayedArrivalLeadCount} late={value.InputSource.RelayedArrivalLateCount}");
                for (int i = 0; i < value.InputSource.RemoteActors.Count; i++)
                {
                    RollbackRemoteActorInputDiagnosticsSnapshot remote = value.InputSource.RemoteActors[i];
                    long frontierGap = (long)remote.ExplicitFrontier - (long)value.InputSource.LocalExplicitFrontier;
                    GUILayout.Label($"Remote input {remote.ActorId}: exact={remote.ExactInputHitCount} predicted={remote.PredictedFallbackCount} arrivalDelta={remote.LastArrivalDeltaTicks} frontier={remote.ExplicitFrontier} gap={frontierGap}");
                }
                GUILayout.Label($"Input provenance: predicted={value.LastPredictedInputHash} canonical={value.LastCanonicalInputHash} applied={value.LastAppliedInputHash}");
                GUILayout.Label($"Snapshot history: {value.SnapshotHistoryCount} [{value.SnapshotHistoryFloor}, {value.SnapshotHistoryCeiling}]");
                GUILayout.Label($"Rollback: count={value.RollbackCount} depth={value.LastRollbackDepth} replayed={value.ReplayedTickCount} recovery={value.RecoveryCount}");
                GUILayout.Label($"Correction: explicit={value.ExplicitCorrectionCount} earliestAffected={value.LastExplicitAffectedTick} provenanceOnly={value.ProvenancePromotionCount}");
                GUILayout.Label($"Hash: tick={value.Network.LastHashTick} world={value.Network.LastWorldHash} kcc={value.Network.LastKccHash}");
                GUILayout.Label($"Recovery: required={value.RequiredRecoveryTick} pending={value.PendingRecoveryTick} requests={value.Network.PendingRecoveryCount} scope={value.Network.LastDesyncScope}");
                GUILayout.Label($"Transport: droppedDatagrams={value.Network.DroppedReceivedDatagrams}");
                GUILayout.Label($"Presentation: keep={value.Output.KeepCount} replace={value.Output.ReplaceCount} cancel={value.Output.CancelCount} confirmed={value.Output.ConfirmedOnlyCommitCount}");
                GUILayout.Label($"Presentation branches: body={value.Presentation.BodyBranchReplacementCount} animation={value.Presentation.AnimationBranchReplacementCount} followerPosition={value.Presentation.FollowerPositionCorrectionMeters:F4} followerYaw={value.Presentation.FollowerYawCorrectionDegrees:F3}");
                DrawPoseProbe();
            }
            else
            {
                GUILayout.Label("Rollback runtime is preparing.");
            }
            DrawActorPositions();
            GUILayout.EndArea();
        }

        void OnDisable()
        {
            for (int i = 0; i < m_PoseProbeTargets.Count; i++)
            {
                AnimationPresentationRuntimeTarget target = m_PoseProbeTargets[i];
                target.RemovePoseWatchInterests(m_PoseProbeOwnerId);
                target.RemoveDiagnosticsInterest(m_PoseProbeOwnerId);
            }
            m_PoseProbeTargets.Clear();
            m_PoseProbeStates.Clear();
        }

        void UpdatePoseProbe()
        {
            IReadOnlyList<AnimationPresentationRuntimeTarget> targets =
                AnimationPresentationRuntimeTargetRegistry.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                AnimationPresentationRuntimeTarget target = targets[i];
                if (!m_PoseProbeStates.TryGetValue(target.RuntimeInstanceId, out PoseProbeState state))
                {
                    state = new PoseProbeState(target.DisplayName);
                    m_PoseProbeStates.Add(target.RuntimeInstanceId, state);
                    m_PoseProbeTargets.Add(target);
                    target.SetDiagnosticsInterest(
                        m_PoseProbeOwnerId,
                        AnimationPresentationDiagnosticsInterest.Capture |
                        AnimationPresentationDiagnosticsInterest.OperationDetail);
                }
                if (!target.TryGetDebugView(out AnimationPresentationDebugView debugView))
                    continue;
                if (!state.WatchesConfigured)
                {
                    IReadOnlyList<AnimationPoseWatchIdentity> watches = BuildPoseWatches(debugView.PosePlan);
                    if (watches.Count == 0)
                        continue;
                    target.SetPoseWatchInterests(m_PoseProbeOwnerId, watches);
                    state.WatchesConfigured = true;
                    continue;
                }
                state.Capture(debugView.PosePlan);
            }
        }

        void DrawPoseProbe()
        {
            foreach (PoseProbeState state in m_PoseProbeStates.Values)
            {
                GUILayout.Label(
                    $"Pose probe {state.DisplayName}: source={state.Source} ik={state.FullBodyIk} final={state.FinalLocal}");
                GUILayout.Label(
                    $"Foot probe: L goal={state.LeftGoalDelta:F4} solved={state.LeftSolvedDelta:F4} | " +
                    $"R goal={state.RightGoalDelta:F4} solved={state.RightSolvedDelta:F4}");
            }
        }

        static IReadOnlyList<AnimationPoseWatchIdentity> BuildPoseWatches(
            AnimationPresentationRuntimeSnapshot snapshot)
        {
            var result = new List<AnimationPoseWatchIdentity>(5);
            AnimationReadOnlyBuffer<AnimationPoseOperationSnapshot> operations = snapshot.Operations;
            for (int i = 0; i < operations.Count; i++)
            {
                AnimationPoseOperationSnapshot operation = operations[i];
                if (operation.Code != CharacterPoseOperationCode.LocalToComponentPose &&
                    operation.Code != CharacterPoseOperationCode.FootPlacement &&
                    operation.Code != CharacterPoseOperationCode.FullBodyIK &&
                    operation.Code != CharacterPoseOperationCode.ComponentToLocalPose)
                {
                    continue;
                }
                result.Add(new AnimationPoseWatchIdentity(
                    operation.GraphId,
                    snapshot.PoseGraphRevision,
                    operation.NodeId,
                    operation.CallSite));
            }
            return result;
        }

        sealed class PoseProbeState
        {
            readonly Dictionary<CharacterPoseOperationCode, CharacterComponentBonePose[]> m_PreviousPoses =
                new Dictionary<CharacterPoseOperationCode, CharacterComponentBonePose[]>();
            bool m_HasPreviousFoot;
            Vector3 m_PreviousLeftGoal;
            Vector3 m_PreviousRightGoal;
            Vector3 m_PreviousLeftSolved;
            Vector3 m_PreviousRightSolved;
            ulong m_LastCompletionIdentity;

            internal PoseProbeState(string displayName) => DisplayName = displayName;

            internal string DisplayName { get; }
            internal bool WatchesConfigured { get; set; }
            internal PoseDelta Source { get; private set; }
            internal PoseDelta FullBodyIk { get; private set; }
            internal PoseDelta FinalLocal { get; private set; }
            internal float LeftGoalDelta { get; private set; }
            internal float RightGoalDelta { get; private set; }
            internal float LeftSolvedDelta { get; private set; }
            internal float RightSolvedDelta { get; private set; }

            internal void Capture(AnimationPresentationRuntimeSnapshot snapshot)
            {
                if (snapshot.CompletionIdentity == m_LastCompletionIdentity)
                    return;
                m_LastCompletionIdentity = snapshot.CompletionIdentity;
                AnimationReadOnlyBuffer<AnimationPoseWatchSnapshot> watches = snapshot.PoseWatches;
                for (int i = 0; i < watches.Count; i++)
                {
                    AnimationPoseWatchSnapshot watch = watches[i];
                    if (watch.Availability != AnimationPoseWatchAvailability.Pose)
                        continue;
                    PoseDelta delta = CapturePose(
                        watch.OperationCode,
                        snapshot.GetPoseWatchComponentPoses(i));
                    switch (watch.OperationCode)
                    {
                        case CharacterPoseOperationCode.LocalToComponentPose:
                            Source = delta;
                            break;
                        case CharacterPoseOperationCode.FullBodyIK:
                            FullBodyIk = delta;
                            break;
                        case CharacterPoseOperationCode.ComponentToLocalPose:
                            FinalLocal = delta;
                            break;
                    }
                }
                CaptureFoot(snapshot.FootIk);
                if (IsAmplifiedJump())
                {
                    Debug.Log(
                        $"[DEBUG-loco-ik] target={DisplayName} completion={snapshot.CompletionIdentity} " +
                        $"source={Source} ik={FullBodyIk} final={FinalLocal} " +
                        $"leftGoal={LeftGoalDelta:R} leftSolved={LeftSolvedDelta:R} " +
                        $"rightGoal={RightGoalDelta:R} rightSolved={RightSolvedDelta:R}");
                }
            }

            PoseDelta CapturePose(
                CharacterPoseOperationCode code,
                AnimationReadOnlyBuffer<CharacterComponentBonePose> poses)
            {
                if (!m_PreviousPoses.TryGetValue(code, out CharacterComponentBonePose[] previous) ||
                    previous.Length != poses.Count)
                {
                    previous = new CharacterComponentBonePose[poses.Count];
                    m_PreviousPoses[code] = previous;
                    for (int i = 0; i < poses.Count; i++)
                        previous[i] = poses[i];
                    return default;
                }
                float maxPosition = 0f;
                float maxRotation = 0f;
                int maxPositionBone = -1;
                int maxRotationBone = -1;
                for (int i = 0; i < poses.Count; i++)
                {
                    CharacterComponentBonePose current = poses[i];
                    float position = Vector3.Distance(previous[i].Position, current.Position);
                    float rotation = Quaternion.Angle(previous[i].Rotation, current.Rotation);
                    if (position > maxPosition)
                    {
                        maxPosition = position;
                        maxPositionBone = i;
                    }
                    if (rotation > maxRotation)
                    {
                        maxRotation = rotation;
                        maxRotationBone = i;
                    }
                    previous[i] = current;
                }
                return new PoseDelta(maxPosition, maxRotation, maxPositionBone, maxRotationBone);
            }

            void CaptureFoot(AnimationFootIkRuntimeSnapshot footIk)
            {
                if (!footIk.IsAvailable)
                    return;
                Vector3 leftGoal = footIk.LeftGoal.ComponentPosition;
                Vector3 rightGoal = footIk.RightGoal.ComponentPosition;
                Vector3 leftSolved = footIk.LeftFoot.SolvedComponentPosition;
                Vector3 rightSolved = footIk.RightFoot.SolvedComponentPosition;
                if (m_HasPreviousFoot)
                {
                    LeftGoalDelta = Vector3.Distance(m_PreviousLeftGoal, leftGoal);
                    RightGoalDelta = Vector3.Distance(m_PreviousRightGoal, rightGoal);
                    LeftSolvedDelta = Vector3.Distance(m_PreviousLeftSolved, leftSolved);
                    RightSolvedDelta = Vector3.Distance(m_PreviousRightSolved, rightSolved);
                }
                m_PreviousLeftGoal = leftGoal;
                m_PreviousRightGoal = rightGoal;
                m_PreviousLeftSolved = leftSolved;
                m_PreviousRightSolved = rightSolved;
                m_HasPreviousFoot = true;
            }

            bool IsAmplifiedJump()
            {
                bool ikPosition = FullBodyIk.PositionDelta > 0.2f &&
                                  FullBodyIk.PositionDelta > Source.PositionDelta * 2.5f;
                bool ikRotation = FullBodyIk.RotationDelta > 60f &&
                                  FullBodyIk.RotationDelta > Source.RotationDelta * 2f;
                return ikPosition || ikRotation ||
                       LeftGoalDelta > 0.2f || RightGoalDelta > 0.2f ||
                       LeftSolvedDelta > 0.2f || RightSolvedDelta > 0.2f;
            }
        }

        readonly struct PoseDelta
        {
            internal PoseDelta(
                float positionDelta,
                float rotationDelta,
                int positionBone,
                int rotationBone)
            {
                PositionDelta = positionDelta;
                RotationDelta = rotationDelta;
                PositionBone = positionBone;
                RotationBone = rotationBone;
            }

            internal float PositionDelta { get; }
            internal float RotationDelta { get; }
            internal int PositionBone { get; }
            internal int RotationBone { get; }
            public override string ToString() =>
                $"{PositionDelta:F4}m[b{PositionBone}]/{RotationDelta:F1}deg[b{RotationBone}]";
        }

        void DrawActorPositions()
        {
            int count = m_Actors == null ? 0 : m_Actors.Length;
            GUILayout.Label($"Actors: {count}");
            for (int i = 0; i < count; i++)
            {
                DeterministicRollbackCharacterHost actor = m_Actors[i];
                if (!actor)
                    continue;
                Vector3 position = actor.VisualPosition;
                GUILayout.Label(
                    $"{actor.ActorId} {(actor.IsLocalActor ? "local" : "remote")} visual=({position.x:F3}, {position.y:F3}, {position.z:F3})");
            }
        }

        DeterministicRollbackCharacterHost FindLocalActor()
        {
            if (m_Actors == null)
                return null;
            for (int i = 0; i < m_Actors.Length; i++)
            {
                if (m_Actors[i] && m_Actors[i].IsLocalActor)
                    return m_Actors[i];
            }
            return null;
        }
    }
}
