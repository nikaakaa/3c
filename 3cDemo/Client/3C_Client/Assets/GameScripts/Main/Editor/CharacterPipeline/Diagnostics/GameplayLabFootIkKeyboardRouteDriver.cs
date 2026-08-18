using System;
using ThirdPersonCamera;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
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
        const float PendingTimeoutSeconds = 60f;

        static bool s_Active;
        static bool s_OwnsSampling;
        static GameplayLabFootIkStairAdPlan s_Plan;
        static GameplayLabFootIkStairAdState s_State;
        static double s_StopTime;
        static string s_LastReport = string.Empty;

        static GameplayLabFootIkKeyboardRouteDriver()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= StartAfterPlayMode;
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
        }

        [MenuItem("Tools/3C/Diagnostics/Foot Landing Stair AD/Start")]
        public static void Start()
        {
            if (s_Active)
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
            if (CharacterFootLandingPredictionSampler.IsCapturing)
                throw new InvalidOperationException("Foot Landing sampling is already active.");
            EditorApplication.isPaused = false;
            ClearPending();
            Scene scene = SceneManager.GetActiveScene();
            GameplayLabFootIkRegressionCourse.Resolve(scene, out Vector3 start, out Vector3 end);
            s_Plan = GameplayLabFootIkStairAdRoute.Create(start, end);
            s_State = GameplayLabFootIkStairAdRoute.CreateState();
            s_StopTime = EditorApplication.timeSinceStartup + SampleSeconds;
            s_Active = true;
            CharacterFootLandingPredictionSampler.StartSampling();
            s_OwnsSampling = true;
            s_LastReport = "Auto walking stairs with A/D...";
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        [MenuItem("Tools/3C/Diagnostics/Foot Landing Stair AD/Stop")]
        public static void StopFromMenu()
        {
            ClearPending();
            Stop();
        }

        public static void Stop()
        {
            if (!s_Active)
                return;
            EditorApplication.update -= Tick;
            ReleaseKeys();
            s_Active = false;
            s_StopTime = 0d;
            if (s_OwnsSampling)
            {
                s_OwnsSampling = false;
                if (CharacterFootLandingPredictionSampler.IsCapturing)
                    CharacterFootLandingPredictionSampler.StopAndSaveSampling();
                CharacterFootLandingStep1Report report =
                    CharacterFootLandingStep1Evaluator.Evaluate(
                        CharacterFootLandingPredictionSampler.LastSavedPath);
                s_LastReport = report.Summary;
                Debug.Log("Foot Landing Stair AD " + report.Summary);
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
                EditorApplication.update -= StartAfterPlayMode;
                EditorApplication.update += StartAfterPlayMode;
                return;
            }
            if (state == PlayModeStateChange.ExitingPlayMode ||
                state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.update -= StartAfterPlayMode;
                ClearPending();
                Stop();
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
                if (host == null || !host.VisualRoot)
                    continue;
                if (string.Equals(host.ActorId, "gameplay-lab-player", StringComparison.Ordinal))
                {
                    chosen = host;
                    break;
                }
                if (chosen == null)
                    chosen = host;
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
                if (host == null || !host.CameraRig)
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
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                throw new InvalidOperationException("GameplayLab stair AD drive requires Keyboard.current.");
            QueueKeys(keyboard, keys.A, keys.D, keys.W, keys.S);
        }

        static void ReleaseKeys()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.added)
                return;
            QueueKeys(keyboard, false, false, false, false);
        }

        static void QueueKeys(Keyboard keyboard, bool a, bool d, bool w, bool s)
        {
            KeyboardState state = default;
            for (int i = 0; i < keyboard.allKeys.Count; i++)
            {
                KeyControl key = keyboard.allKeys[i];
                if (key != null && key.isPressed)
                    state.Press(key.keyCode);
            }
            state.Set(Key.A, a);
            state.Set(Key.D, d);
            state.Set(Key.W, w);
            state.Set(Key.S, s);
            InputSystem.QueueStateEvent(keyboard, state);
        }
    }
}
