using System;
using ThirdPersonCamera;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [InitializeOnLoad]
    public static class GameplayLabFootIkKeyboardRouteDriver
    {
        const double SampleSeconds = 45d;
        const string PendingKey = "ThirdPerson.GameplayLab.StairAd.Pending.v2";
        const string PendingDeadlineKey = "ThirdPerson.GameplayLab.StairAd.PendingDeadline.v2";
        const string RestartPendingKey = "ThirdPerson.GameplayLab.StairAd.RestartPending.v1";
        const string CompletedKey = "ThirdPerson.GameplayLab.StairAd.Completed.v1";
        const string GameplayLabPlayerActorId = "gameplay-lab-player";
        const float PendingTimeoutSeconds = 60f;

        static bool s_Active;
        static bool s_WaitingForSampling;
        static bool s_OwnsSampling;
        static GameplayLabFootIkStairAdPlan s_Plan;
        static GameplayLabFootIkStairAdState s_State;
        static double s_StopTime;
        static string s_LastReport = string.Empty;
        static bool s_HasCompletedRun;
        static Keyboard s_RouteKeyboard;

        static GameplayLabFootIkKeyboardRouteDriver()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload += ReleaseRouteKeyboard;
            EditorApplication.update -= StartAfterPlayMode;
            EditorApplication.update -= StartAfterSampling;
            if (IsPending)
                EditorApplication.update += StartAfterPlayMode;
        }

        public static bool IsActive => s_Active;
        public static bool IsPending => EditorPrefs.GetBool(PendingKey, false);
        public static GameplayLabFootIkStairAdPhase Phase => s_State.Phase;
        public static int Lap => s_State.Lap;
        public static double SampleSecondsValue => SampleSeconds;
        public static string LastReport => s_LastReport;

        public static void ArmPending()
        {
            EditorPrefs.SetBool(PendingKey, true);
            EditorPrefs.SetFloat(
                PendingDeadlineKey,
                (float)EditorApplication.timeSinceStartup + PendingTimeoutSeconds);
            s_LastReport = "Starting Gameplay Lab...";
        }

        public static void ClearPending()
        {
            EditorPrefs.SetBool(PendingKey, false);
            EditorPrefs.DeleteKey(PendingDeadlineKey);
            EditorPrefs.SetBool(RestartPendingKey, false);
        }

        [MenuItem("Tools/3C/Diagnostics/Foot Landing Stair AD/Start")]
        public static void Start()
        {
            if (s_Active || s_WaitingForSampling)
                return;
            if (!EditorApplication.isPlaying)
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;
                IGameplayLabLauncherOperations operations = GameplayLabLauncherRegistry.Operations;
                if (operations == null)
                    throw new InvalidOperationException("GameplayLab launcher operations are not registered.");
                try
                {
                    GameplayLabLauncherState launcherState = operations.ReadState();
                    EditorPrefs.SetBool(CompletedKey, false);
                    ArmPending();
                    EditorApplication.update -= StartAfterPlayMode;
                    EditorApplication.update += StartAfterPlayMode;
                    operations.Play(launcherState.SelectedVariantIndex);
                }
                catch
                {
                    ClearPending();
                    throw;
                }
                return;
            }
            if (s_HasCompletedRun || EditorPrefs.GetBool(CompletedKey, false))
            {
                RestartAfterCompletedRun();
                return;
            }
            if (!IsPending &&
                !CharacterFootLandingPredictionSampler.IsCapturing &&
                AnimationPresentationRuntimeTargetRegistry.Targets.Count > 0)
            {
                RestartAfterCompletedRun();
                return;
            }
            if (CharacterFootLandingPredictionSampler.IsCapturing)
                throw new InvalidOperationException("Foot Landing sampling is already active.");
            EditorApplication.isPaused = false;
            Scene scene = SceneManager.GetActiveScene();
            GameplayLabFootIkRegressionCourse.Resolve(scene, out Vector3 start, out Vector3 end);
            s_Plan = GameplayLabFootIkStairAdRoute.Create(start, end);
            s_State = GameplayLabFootIkStairAdRoute.CreateState();
            CharacterFootLandingPredictionSampler.StartSampling();
            s_OwnsSampling = true;
            if (CharacterFootLandingPredictionSampler.IsStartPending)
            {
                s_WaitingForSampling = true;
                s_LastReport = "Waiting for Gameplay Lab player...";
                EditorApplication.update -= StartAfterSampling;
                EditorApplication.update += StartAfterSampling;
                return;
            }
            BeginDriving();
        }

        static void BeginDriving()
        {
            AcquireRouteKeyboard();
            s_StopTime = EditorApplication.timeSinceStartup + SampleSeconds;
            s_Active = true;
            s_WaitingForSampling = false;
            ClearPending();
            s_LastReport = "Auto walking stairs with A/D...";
            EditorApplication.update -= StartAfterSampling;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        static void StartAfterSampling()
        {
            if (!s_WaitingForSampling)
            {
                EditorApplication.update -= StartAfterSampling;
                return;
            }
            if (CharacterFootLandingPredictionSampler.IsStartPending)
                return;
            if (CharacterFootLandingPredictionSampler.IsCapturing)
            {
                BeginDriving();
                return;
            }
            EditorApplication.update -= StartAfterSampling;
            s_WaitingForSampling = false;
            s_OwnsSampling = false;
            ClearPending();
            s_LastReport = string.IsNullOrEmpty(CharacterFootLandingPredictionSampler.LastStartFailure)
                ? "Foot Landing sampling start was canceled."
                : CharacterFootLandingPredictionSampler.LastStartFailure;
        }

        [MenuItem("Tools/3C/Diagnostics/Foot Landing Stair AD/Stop")]
        public static void StopFromMenu()
        {
            ClearPending();
            Stop();
        }

        public static void Stop()
        {
            if (!s_Active && !s_WaitingForSampling)
                return;
            bool completedCapture = s_Active;
            EditorApplication.update -= Tick;
            EditorApplication.update -= StartAfterSampling;
            ReleaseKeys();
            ReleaseRouteKeyboard();
            s_Active = false;
            s_WaitingForSampling = false;
            s_StopTime = 0d;
            if (s_OwnsSampling)
            {
                s_OwnsSampling = false;
                if (CharacterFootLandingPredictionSampler.IsCapturing ||
                    CharacterFootLandingPredictionSampler.IsStartPending)
                {
                    CharacterFootLandingPredictionSampler.StopAndSaveSampling();
                }
                if (!completedCapture ||
                    string.IsNullOrEmpty(CharacterFootLandingPredictionSampler.LastSavedPath))
                {
                    s_LastReport = "Foot Landing sampling stopped before recording started.";
                    return;
                }
                CharacterFootLandingStep1Report report =
                    CharacterFootLandingStep1Evaluator.Evaluate(
                        CharacterFootLandingPredictionSampler.LastSavedPath);
                s_LastReport = report.Summary;
                s_HasCompletedRun = true;
                EditorPrefs.SetBool(CompletedKey, true);
                Debug.Log("Foot Landing Stair AD " + report.Summary);
            }
        }

        static void RestartAfterCompletedRun()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorPrefs.SetBool(RestartPendingKey, true);
                ArmPending();
                s_HasCompletedRun = false;
                EditorApplication.ExitPlaymode();
            }
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && IsPending)
            {
                EditorApplication.update -= StartAfterPlayMode;
                EditorApplication.update += StartAfterPlayMode;
                return;
            }
            if (state == PlayModeStateChange.EnteredEditMode && IsPending)
            {
                if (EditorPrefs.GetBool(RestartPendingKey, false))
                {
                    EditorApplication.update -= RestartAfterEditMode;
                    EditorApplication.update += RestartAfterEditMode;
                    return;
                }
                EditorApplication.update -= StartAfterPlayMode;
                EditorApplication.update += StartAfterPlayMode;
                return;
            }
            if (state == PlayModeStateChange.ExitingPlayMode ||
                state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.update -= StartAfterPlayMode;
                if (!EditorPrefs.GetBool(RestartPendingKey, false))
                    ClearPending();
                Stop();
            }
        }

        static void RestartAfterEditMode()
        {
            if (!EditorPrefs.GetBool(RestartPendingKey, false))
            {
                EditorApplication.update -= RestartAfterEditMode;
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            try
            {
                IGameplayLabLauncherOperations operations = GameplayLabLauncherRegistry.Operations;
                if (operations == null)
                    throw new InvalidOperationException("GameplayLab launcher operations are not registered.");
                GameplayLabLauncherState launcherState = operations.ReadState();
                EditorPrefs.SetBool(CompletedKey, false);
                EditorPrefs.SetBool(RestartPendingKey, false);
                operations.Play(launcherState.SelectedVariantIndex);
                EditorApplication.update -= RestartAfterEditMode;
            }
            catch (Exception exception)
            {
                if (EditorApplication.timeSinceStartup <
                    EditorPrefs.GetFloat(PendingDeadlineKey, 0f))
                    return;
                EditorApplication.update -= RestartAfterEditMode;
                ClearPending();
                s_LastReport = exception.Message;
                Debug.LogException(exception);
            }
        }

        static void StartAfterPlayMode()
        {
            if (!IsPending || s_Active)
            {
                EditorApplication.update -= StartAfterPlayMode;
                return;
            }
            if (!EditorApplication.isPlaying)
            {
                if (EditorApplication.timeSinceStartup >=
                    EditorPrefs.GetFloat(PendingDeadlineKey, 0f) &&
                    !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorApplication.update -= StartAfterPlayMode;
                    ClearPending();
                    s_LastReport = "Gameplay Lab PlayMode start timed out.";
                }
                return;
            }
            try
            {
                GameplayLabFootIkRegressionCourse.Resolve(
                    SceneManager.GetActiveScene(),
                    out _,
                    out _);
                Start();
                EditorApplication.update -= StartAfterPlayMode;
            }
            catch (Exception exception)
            {
                if (EditorApplication.timeSinceStartup <
                    EditorPrefs.GetFloat(PendingDeadlineKey, 0f))
                    return;
                EditorApplication.update -= StartAfterPlayMode;
                ClearPending();
                s_LastReport = exception.Message;
                Debug.LogException(exception);
            }
        }

        static void Tick()
        {
            if (!s_Active)
                return;
            if (!EditorApplication.isPlaying ||
                EditorApplication.timeSinceStartup >= s_StopTime)
            {
                Stop();
                return;
            }
            if (!TryResolveActor(out Vector3 position, out CameraBasisSnapshot basis))
            {
                ReleaseKeys();
                return;
            }
            Vector3 target = GameplayLabFootIkStairAdRoute.Tick(ref s_State, in s_Plan, position);
            Vector2 world = GameplayLabFootIkStairAdRoute.WorldDirection(position, target);
            Vector2 camera = ToCameraRelative(world, basis);
            ApplyKeys(GameplayLabFootIkKeyboardMove.FromCameraRelative(camera.x, camera.y));
        }

        static bool TryResolveActor(out Vector3 position, out CameraBasisSnapshot basis)
        {
            position = default;
            basis = default;
            CharacterPipelineHost[] hosts = UnityEngine.Object.FindObjectsByType<CharacterPipelineHost>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            CharacterPipelineHost chosen = null;
            for (int i = 0; i < hosts.Length; i++)
            {
                CharacterPipelineHost host = hosts[i];
                if (host == null || !host.VisualRoot ||
                    !string.Equals(host.ActorId, GameplayLabPlayerActorId, StringComparison.Ordinal))
                    continue;
                chosen = host;
                break;
            }
            if (chosen != null && chosen.CameraRig)
            {
                position = chosen.VisualRoot.position;
                basis = chosen.CameraRig.BasisSnapshot;
                return basis.Valid;
            }
            FixedCharacterHost[] fixedHosts = UnityEngine.Object.FindObjectsByType<FixedCharacterHost>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (int i = 0; i < fixedHosts.Length; i++)
            {
                FixedCharacterHost host = fixedHosts[i];
                if (host == null || !host.CameraRig ||
                    !string.Equals(host.ActorId.Value, GameplayLabPlayerActorId, StringComparison.Ordinal))
                    continue;
                position = host.VisualPosition;
                basis = host.CameraRig.BasisSnapshot;
                return basis.Valid;
            }
            return false;
        }

        static Vector2 ToCameraRelative(Vector2 worldMovement, CameraBasisSnapshot basis)
        {
            if (worldMovement.sqrMagnitude <= 0.000001f)
                return Vector2.zero;
            if (!basis.Valid)
                throw new InvalidOperationException("GameplayLab stair AD drive requires a valid camera basis.");
            Vector3 forward = Vector3.ProjectOnPlane(basis.PlanarForward, Vector3.up);
            Vector3 right = Vector3.ProjectOnPlane(basis.PlanarRight, Vector3.up);
            if (forward.sqrMagnitude <= 0.000001f || right.sqrMagnitude <= 0.000001f)
                throw new InvalidOperationException("GameplayLab stair AD drive received a degenerate camera basis.");
            Vector3 world = new Vector3(worldMovement.x, 0f, worldMovement.y);
            Vector2 cameraRelative = new Vector2(
                Vector3.Dot(world, right.normalized),
                Vector3.Dot(world, forward.normalized));
            return cameraRelative.sqrMagnitude > 1f ? cameraRelative.normalized : cameraRelative;
        }

        static void ApplyKeys(GameplayLabFootIkKeyboardMove keys)
        {
            AcquireRouteKeyboard();
            QueueKeys(s_RouteKeyboard, keys.A, keys.D, keys.W, keys.S);
        }

        static void ReleaseKeys()
        {
            if (s_RouteKeyboard == null || !s_RouteKeyboard.added)
                return;
            QueueKeys(s_RouteKeyboard, false, false, false, false);
        }

        static void AcquireRouteKeyboard()
        {
            if (s_RouteKeyboard != null && s_RouteKeyboard.added)
                return;
            s_RouteKeyboard = InputSystem.AddDevice<Keyboard>("FootIkStairAdKeyboard");
        }

        static void ReleaseRouteKeyboard()
        {
            if (s_RouteKeyboard != null && s_RouteKeyboard.added)
                InputSystem.RemoveDevice(s_RouteKeyboard);
            s_RouteKeyboard = null;
        }

        static void QueueKeys(Keyboard keyboard, bool a, bool d, bool w, bool s)
        {
            KeyboardState state = default;
            state.Set(Key.A, a);
            state.Set(Key.D, d);
            state.Set(Key.W, w);
            state.Set(Key.S, s);
            InputSystem.QueueStateEvent(keyboard, state);
        }
    }
}
