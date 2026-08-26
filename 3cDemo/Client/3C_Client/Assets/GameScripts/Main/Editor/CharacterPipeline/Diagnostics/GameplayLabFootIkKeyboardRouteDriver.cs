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
    public enum GameplayLabFootIkAutomaticRouteMode : byte
    {
        StairAdStress = 1,
        StairStraight = 2
    }

    [InitializeOnLoad]
    public static class GameplayLabFootIkKeyboardRouteDriver
    {
        const double StressSampleSeconds = 45d;
        const double StraightSampleSeconds = 24d;
        const string PendingKey = "ThirdPerson.GameplayLab.StairAd.Pending.v2";
        const string PendingDeadlineKey = "ThirdPerson.GameplayLab.StairAd.PendingDeadline.v2";
        const string PendingModeKey = "ThirdPerson.GameplayLab.FootIkRoute.PendingMode.v1";
        const string RestartPendingKey = "ThirdPerson.GameplayLab.StairAd.RestartPending.v1";
        const string CompletedKey = "ThirdPerson.GameplayLab.StairAd.Completed.v1";
        const string GameplayLabPlayerActorId = "gameplay-lab-player";
        const float PendingTimeoutSeconds = 60f;

        static bool s_Active;
        static bool s_WaitingForSampling;
        static bool s_OwnsSampling;
        static GameplayLabFootIkAutomaticRouteMode s_Mode =
            GameplayLabFootIkAutomaticRouteMode.StairAdStress;
        static GameplayLabFootIkStairAdPlan s_Plan;
        static GameplayLabFootIkStairAdState s_State;
        static GameplayLabFootIkStairStraightPlan s_StraightPlan;
        static GameplayLabFootIkStairStraightState s_StraightState;
        static double s_StopTime;
        static string s_LastDiagnosticSummary = string.Empty;
        static bool s_HasCompletedRun;
        static Keyboard s_RouteKeyboard;
        static bool s_RouteKeyboardReleasePending;

        static GameplayLabFootIkKeyboardRouteDriver()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload += ReleaseRouteKeyboard;
            EditorApplication.update -= StartAfterPlayMode;
            EditorApplication.update -= StartAfterSampling;
            if (IsPending)
            {
                s_Mode = ReadPendingMode();
                EditorApplication.update += StartAfterPlayMode;
            }
        }

        public static bool IsActive => s_Active;
        public static bool IsPending => EditorPrefs.GetBool(PendingKey, false);
        public static GameplayLabFootIkAutomaticRouteMode Mode => s_Mode;
        public static string PhaseName
        {
            get
            {
                if (!s_Active && !s_WaitingForSampling && !IsPending)
                    return "Idle";
                return s_Mode == GameplayLabFootIkAutomaticRouteMode.StairStraight
                    ? s_StraightState.Phase.ToString()
                    : s_State.Phase.ToString();
            }
        }
        public static int Lap => s_Mode == GameplayLabFootIkAutomaticRouteMode.StairStraight
            ? s_StraightState.Lap
            : s_State.Lap;
        public static double SampleSecondsValue => s_Mode == GameplayLabFootIkAutomaticRouteMode.StairStraight
            ? StraightSampleSeconds
            : StressSampleSeconds;
        public static string LastDiagnosticSummary => s_LastDiagnosticSummary;

        public static void ArmPending() =>
            ArmPending(GameplayLabFootIkAutomaticRouteMode.StairAdStress);

        public static void ArmPending(GameplayLabFootIkAutomaticRouteMode mode)
        {
            RequireMode(mode);
            EditorPrefs.SetBool(PendingKey, true);
            EditorPrefs.SetFloat(
                PendingDeadlineKey,
                (float)EditorApplication.timeSinceStartup + PendingTimeoutSeconds);
            EditorPrefs.SetInt(PendingModeKey, (int)mode);
            s_Mode = mode;
            s_LastDiagnosticSummary = $"Starting Gameplay Lab for {ModeLabel(mode)}...";
        }

        public static void ClearPending()
        {
            EditorPrefs.SetBool(PendingKey, false);
            EditorPrefs.DeleteKey(PendingDeadlineKey);
            EditorPrefs.DeleteKey(PendingModeKey);
            EditorPrefs.SetBool(RestartPendingKey, false);
        }

        [MenuItem("Tools/3C/Diagnostics/Foot Landing Stair AD/Start")]
        public static void Start() =>
            Start(GameplayLabFootIkAutomaticRouteMode.StairAdStress);

        [MenuItem("Tools/3C/Diagnostics/Foot Landing Stair Straight/Start")]
        public static void StartStraight() =>
            Start(GameplayLabFootIkAutomaticRouteMode.StairStraight);

        public static void Start(GameplayLabFootIkAutomaticRouteMode mode)
        {
            RequireMode(mode);
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
                    ArmPending(mode);
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
                RestartAfterCompletedRun(mode);
                return;
            }
            if (!IsPending &&
                !CharacterFootLandingPredictionSampler.IsCapturing &&
                AnimationPresentationRuntimeTargetRegistry.Targets.Count > 0)
            {
                RestartAfterCompletedRun(mode);
                return;
            }
            if (CharacterFootLandingPredictionSampler.IsCapturing)
                throw new InvalidOperationException("Foot Landing sampling is already active.");
            EditorApplication.isPaused = false;
            Scene scene = SceneManager.GetActiveScene();
            GameplayLabFootIkRegressionCourse.Resolve(scene, out Vector3 start, out Vector3 end);
            s_Mode = mode;
            if (mode == GameplayLabFootIkAutomaticRouteMode.StairStraight)
            {
                s_StraightPlan = GameplayLabFootIkStairStraightRoute.Create(start, end);
                s_StraightState = GameplayLabFootIkStairStraightRoute.CreateState();
            }
            else
            {
                s_Plan = GameplayLabFootIkStairAdRoute.Create(start, end);
                s_State = GameplayLabFootIkStairAdRoute.CreateState();
            }
            CharacterFootLandingPredictionSampler.StartSampling();
            s_OwnsSampling = true;
            if (CharacterFootLandingPredictionSampler.IsStartPending)
            {
                s_WaitingForSampling = true;
                s_LastDiagnosticSummary = "Waiting for Gameplay Lab player...";
                EditorApplication.update -= StartAfterSampling;
                EditorApplication.update += StartAfterSampling;
                return;
            }
            BeginDriving();
        }

        static void BeginDriving()
        {
            AcquireRouteKeyboard();
            s_StopTime = EditorApplication.timeSinceStartup + SampleSecondsValue;
            s_Active = true;
            s_WaitingForSampling = false;
            ClearPending();
            s_LastDiagnosticSummary = s_Mode == GameplayLabFootIkAutomaticRouteMode.StairStraight
                ? "Auto walking straight up and down stairs..."
                : "Auto walking stairs with A/D stress input...";
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
            s_LastDiagnosticSummary = string.IsNullOrEmpty(CharacterFootLandingPredictionSampler.LastStartFailure)
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
                    s_LastDiagnosticSummary = "Foot Landing sampling stopped before recording started.";
                    return;
                }
                if (string.IsNullOrEmpty(
                        CharacterFootLandingPredictionSampler.LastSavedFactsPath))
                {
                    s_LastDiagnosticSummary =
                        "Foot Landing samples were sealed but facts.json was not published.";
                    return;
                }
                if (string.IsNullOrEmpty(
                        CharacterFootLandingPredictionSampler.LastSavedDiagnosisDirectory) ||
                    !System.IO.Directory.Exists(
                        CharacterFootLandingPredictionSampler.LastSavedDiagnosisDirectory))
                {
                    s_LastDiagnosticSummary =
                        "Foot Landing facts were published but diagnoses/ was not published.";
                    return;
                }
                s_LastDiagnosticSummary =
                    CharacterFootLandingPredictionSampler.LastDiagnosticSummary;
                s_HasCompletedRun = true;
                EditorPrefs.SetBool(CompletedKey, true);
                Debug.Log(
                    $"Foot Landing {ModeLabel(s_Mode)} " +
                    $"Samples={CharacterFootLandingPredictionSampler.LastSavedPath}, " +
                    $"Facts={CharacterFootLandingPredictionSampler.LastSavedFactsPath}, " +
                    $"Diagnoses={CharacterFootLandingPredictionSampler.LastSavedDiagnosisDirectory}, " +
                    $"Summary={s_LastDiagnosticSummary}");
            }
        }

        static void RestartAfterCompletedRun(GameplayLabFootIkAutomaticRouteMode mode)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorPrefs.SetBool(RestartPendingKey, true);
                ArmPending(mode);
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
                s_LastDiagnosticSummary = exception.Message;
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
                    s_LastDiagnosticSummary = "Gameplay Lab PlayMode start timed out.";
                }
                return;
            }
            try
            {
                GameplayLabFootIkRegressionCourse.Resolve(
                    SceneManager.GetActiveScene(),
                    out _,
                    out _);
                Start(ReadPendingMode());
                EditorApplication.update -= StartAfterPlayMode;
            }
            catch (Exception exception)
            {
                if (EditorApplication.timeSinceStartup <
                    EditorPrefs.GetFloat(PendingDeadlineKey, 0f))
                    return;
                EditorApplication.update -= StartAfterPlayMode;
                ClearPending();
                s_LastDiagnosticSummary = exception.Message;
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
            Vector2 world;
            if (s_Mode == GameplayLabFootIkAutomaticRouteMode.StairStraight)
            {
                world = GameplayLabFootIkStairStraightRoute.Tick(
                    ref s_StraightState,
                    in s_StraightPlan,
                    position);
            }
            else
            {
                Vector3 target = GameplayLabFootIkStairAdRoute.Tick(ref s_State, in s_Plan, position);
                world = GameplayLabFootIkStairAdRoute.WorldDirection(position, target);
            }
            Vector2 camera = ToCameraRelative(world, basis);
            ApplyKeys(GameplayLabFootIkKeyboardMove.FromCameraRelative(camera.x, camera.y));
        }

        public static bool TryGetPlayerPosition(out Vector3 position)
        {
            bool available = TryResolveActor(out position, out _);
            return available;
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
            CancelRouteKeyboardRelease();
            if (s_RouteKeyboard != null && s_RouteKeyboard.added)
                return;
            s_RouteKeyboard = InputSystem.AddDevice<Keyboard>("FootIkStairAdKeyboard");
        }

        static void ReleaseRouteKeyboard()
        {
            if (s_RouteKeyboard == null || !s_RouteKeyboard.added)
            {
                CancelRouteKeyboardRelease();
                s_RouteKeyboard = null;
                return;
            }
            if (s_RouteKeyboardReleasePending)
                return;
            s_RouteKeyboardReleasePending = true;
            InputSystem.onAfterUpdate += CompleteRouteKeyboardRelease;
        }

        static void CompleteRouteKeyboardRelease()
        {
            if (s_RouteKeyboard == null || !s_RouteKeyboard.added)
            {
                CancelRouteKeyboardRelease();
                s_RouteKeyboard = null;
                return;
            }
            if (s_RouteKeyboard.aKey.isPressed || s_RouteKeyboard.dKey.isPressed ||
                s_RouteKeyboard.wKey.isPressed || s_RouteKeyboard.sKey.isPressed)
            {
                return;
            }
            InputSystem.RemoveDevice(s_RouteKeyboard);
            s_RouteKeyboard = null;
            CancelRouteKeyboardRelease();
        }

        static void CancelRouteKeyboardRelease()
        {
            if (!s_RouteKeyboardReleasePending)
                return;
            InputSystem.onAfterUpdate -= CompleteRouteKeyboardRelease;
            s_RouteKeyboardReleasePending = false;
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

        static GameplayLabFootIkAutomaticRouteMode ReadPendingMode()
        {
            var mode = (GameplayLabFootIkAutomaticRouteMode)EditorPrefs.GetInt(
                PendingModeKey,
                (int)GameplayLabFootIkAutomaticRouteMode.StairAdStress);
            RequireMode(mode);
            return mode;
        }

        static void RequireMode(GameplayLabFootIkAutomaticRouteMode mode)
        {
            if (mode != GameplayLabFootIkAutomaticRouteMode.StairAdStress &&
                mode != GameplayLabFootIkAutomaticRouteMode.StairStraight)
            {
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Foot IK automatic route mode is invalid.");
            }
        }

        static string ModeLabel(GameplayLabFootIkAutomaticRouteMode mode) =>
            mode == GameplayLabFootIkAutomaticRouteMode.StairStraight
                ? "Stair Straight"
                : "Stair AD Stress";
    }
}
