using System;
using ThirdPersonCamera;
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
        const float SampleSeconds = 45d;

        static bool s_Active;
        static GameplayLabFootIkStairAdPlan s_Plan;
        static GameplayLabFootIkStairAdState s_State;
        static double s_StopTime;

        static GameplayLabFootIkKeyboardRouteDriver()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public static bool IsActive => s_Active;
        public static GameplayLabFootIkStairAdPhase Phase => s_State.Phase;
        public static int Lap => s_State.Lap;
        public static double SampleSecondsValue => SampleSeconds;

        public static void Start()
        {
            if (s_Active)
                return;
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("GameplayLab stair AD drive requires Play Mode.");
            Scene scene = SceneManager.GetActiveScene();
            GameplayLabFootIkRegressionCourse.Resolve(scene, out Vector3 start, out Vector3 end);
            s_Plan = GameplayLabFootIkStairAdRoute.Create(start, end);
            s_State = GameplayLabFootIkStairAdRoute.CreateState();
            s_StopTime = EditorApplication.timeSinceStartup + SampleSeconds;
            s_Active = true;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        public static void Stop()
        {
            if (!s_Active)
                return;
            EditorApplication.update -= Tick;
            ReleaseKeys();
            s_Active = false;
            s_StopTime = 0d;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode ||
                state == PlayModeStateChange.EnteredEditMode)
                Stop();
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
            SetKey(keyboard.aKey, keys.A);
            SetKey(keyboard.dKey, keys.D);
            SetKey(keyboard.wKey, keys.W);
            SetKey(keyboard.sKey, keys.S);
        }

        static void ReleaseKeys()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.added)
                return;
            SetKey(keyboard.aKey, false);
            SetKey(keyboard.dKey, false);
            SetKey(keyboard.wKey, false);
            SetKey(keyboard.sKey, false);
        }

        static void SetKey(KeyControl key, bool pressed)
        {
            InputState.Change(key, pressed ? 1f : 0f, InputState.currentUpdateType);
        }
    }
}
