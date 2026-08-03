using ThirdPersonSimulation.DeterministicRollback;
using ThirdPersonGameplay.Tick;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.DeterministicRollback
{
    [DisallowMultipleComponent]
    public sealed class DeterministicRollbackDemoStatusOverlay : MonoBehaviour
    {
        [SerializeField] DeterministicRollbackCharacterHost[] m_Actors;
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
                GUILayout.Label($"Timing: offensiveDelay={value.OffensiveRequestDelayTicks} confirmationDelay={value.ConfirmationDelayTicks}");
                GUILayout.Label($"Input history: {value.InputHistoryCount} [{value.InputHistoryFloor}, {value.InputHistoryCeiling}] late={value.LateInputCount} lastLate={value.LastLateInputTick}");
                GUILayout.Label($"Offensive pending: count={value.InputSource.Local.PendingOffensiveRequestCount} oldestCapture={value.InputSource.Local.OldestCaptureTick} eligible={value.InputSource.Local.OldestEligibleTick}");
                GUILayout.Label($"Relay arrival: count={value.InputSource.RelayedArrivalCount} delta={value.InputSource.LastRelayedArrivalDeltaTicks} lead={value.InputSource.RelayedArrivalLeadCount} late={value.InputSource.RelayedArrivalLateCount}");
                for (int i = 0; i < value.InputSource.RemoteActors.Count; i++)
                {
                    RollbackRemoteActorInputDiagnosticsSnapshot remote = value.InputSource.RemoteActors[i];
                    GUILayout.Label($"Remote input {remote.ActorId}: exact={remote.ExactInputHitCount} predicted={remote.PredictedFallbackCount} arrivalDelta={remote.LastArrivalDeltaTicks}");
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
            }
            else
            {
                GUILayout.Label("Rollback runtime is preparing.");
            }
            DrawActorPositions();
            GUILayout.EndArea();
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
