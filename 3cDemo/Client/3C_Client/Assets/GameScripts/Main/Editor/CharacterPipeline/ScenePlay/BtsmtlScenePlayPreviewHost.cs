using System;
using System.Threading;
using System.Threading.Tasks;
using ThirdPerson.ProductStartup;
using ThirdPersonCamera;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using ThirdPersonGameplay.Tick;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonCharacter.Pipeline.Editor.ScenePlay
{
    [InitializeOnLoad]
    sealed class BtsmtlScenePlayPreviewHost
    {
        internal static readonly BtsmtlScenePlayPreviewHost Shared = new BtsmtlScenePlayPreviewHost();
        internal event Action Changed;
        internal BtsmtlScenePlayPreviewRenderer Renderer { get; } = new BtsmtlScenePlayPreviewRenderer();
        Scene m_Scene;
        GameObject m_Root;
        FixedCharacterHost[] m_Actors = Array.Empty<FixedCharacterHost>();
        CancellationTokenSource m_Cancellation;
        GameplayTickSystem m_Tick;
        double m_LastUpdate;

        static BtsmtlScenePlayPreviewHost()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Shared.Close;
            EditorApplication.quitting += Shared.Close;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingEditMode)
                    Shared.Close();
            };
        }

        internal BtsmtlScenePlayProfile Profile { get; private set; }
        internal SimulationSessionHost Session { get; private set; }
        internal FixedCharacterHost Actor { get; private set; }
        internal bool IsOpen => m_Scene.IsValid();
        internal bool IsReady => Session && Session.LifecycleState == SimulationSessionLifecycleState.Active;
        internal string Error { get; private set; } = string.Empty;
        internal GameplayTickDriveStatusSnapshot DriveStatus => m_Tick.DriveStatus;

        internal async Task OpenAsync(BtsmtlScenePlayProfile profile)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || IsOpen || GameplayTickSystem.IsInitialized)
                throw new InvalidOperationException("隐藏预览需要 Edit Mode，且不能与已有 Tick 宿主同时启动。");
            if (!profile || !profile.IsValid)
                throw new InvalidOperationException("Preview Profile 必须引用正式装配 Prefab、Context 和 Actor。");
            Error = string.Empty;
            Profile = profile;
            m_Cancellation = new CancellationTokenSource();
            CancellationToken cancellation = m_Cancellation.Token;
            try
            {
                m_Scene = EditorSceneManager.NewPreviewScene();
                m_Root = (GameObject)PrefabUtility.InstantiatePrefab(profile.AssemblyPrefab, m_Scene);
                m_Root.hideFlags = HideFlags.HideAndDontSave;
                SimulationSessionHost[] sessions = m_Root.GetComponentsInChildren<SimulationSessionHost>(true);
                for (int i = 0; i < sessions.Length; i++)
                {
                    if (sessions[i].Composition.SessionId != profile.ContextId)
                        continue;
                    if (Session)
                        throw new InvalidOperationException("装配 Prefab 包含重复 Context。");
                    Session = sessions[i];
                }
                if (!Session)
                    throw new InvalidOperationException("装配 Prefab 中缺少 Profile 指定的 Context。");
                Changed?.Invoke();
                ProjectSceneResourceHost[] resources = m_Root.GetComponentsInChildren<ProjectSceneResourceHost>(true);
                for (int i = 0; i < resources.Length; i++)
                    await resources[i].PrepareAsync(cancellation);
                cancellation.ThrowIfCancellationRequested();
                var settings = new GameplayTickSettings(Session.TickRate,
                    GameplayTickSettings.DefaultMaxCatchUpTicks,
                    GameplayAccumulatorOverflowPolicy.PreserveRemainder,
                    GameplayTickTimeSource.Unscaled);
                GameplayTickSystem.Initialize(settings);
                m_Tick = GameplayTickSystem.Current;
                m_Tick.Enqueue(GameplayTickDriveCommand.SetPresentationClock(
                    GameplayPresentationDebugClockMode.LogicLockedPresentation));
                CinemachineCameraRigAdapter[] cameras = m_Root.GetComponentsInChildren<CinemachineCameraRigAdapter>(true);
                for (int i = 0; i < cameras.Length; i++)
                    cameras[i].Initialize();
                m_Actors = m_Root.GetComponentsInChildren<FixedCharacterHost>(true);
                for (int i = 0; i < m_Actors.Length; i++)
                {
                    FixedCharacterHost actor = m_Actors[i];
                    if (actor.SessionHost != Session)
                        throw new InvalidOperationException("预览装配中的角色必须属于当前唯一 Session。");
                    actor.Initialize();
                    if (actor.ActorId.Value != profile.DefaultActorId)
                        continue;
                    if (Actor)
                        throw new InvalidOperationException("预览 Context 包含重复 Actor。");
                    Actor = actor;
                }
                if (!Actor)
                    throw new InvalidOperationException("预览 Context 缺少 Profile 指定的 Actor。");
                Camera outputCamera = Actor.CameraRig.OutputCamera;
                outputCamera.enabled = false;
                outputCamera.scene = m_Scene;
                outputCamera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(m_Scene);
                Session.Activate();
                m_LastUpdate = EditorApplication.timeSinceStartup;
                EditorApplication.update += Update;
                Changed?.Invoke();
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                Error = exception.Message;
                Close();
            }
        }

        internal void Enqueue(GameplayTickDriveCommand command) => m_Tick.Enqueue(command);

        internal void RefreshPreviewImage()
        {
            if (IsReady)
                Renderer.Render(Actor.CameraRig.OutputCamera, m_Tick.DriveStatus.RenderFrame);
        }

        void Update()
        {
            if (!IsOpen || EditorApplication.isCompiling)
                return;
            double now = EditorApplication.timeSinceStartup;
            float delta = (float)(now - m_LastUpdate);
            m_LastUpdate = now;
            bool preparing = Session.LifecycleState is SimulationSessionLifecycleState.Uninitialized or
                SimulationSessionLifecycleState.Preparing;
            GameplayTickDriveStatusSnapshot drive = m_Tick.DriveStatus;
            if (!preparing && drive.Mode == GameplayTickDriveMode.Paused && drive.PendingCommandCount == 0)
                return;
            try
            {
                if (preparing)
                    m_Tick.Enqueue(GameplayTickDriveCommand.Step(1));
                m_Tick.FrameUpdate(delta, delta);
                if (m_Tick.DriveStatus.PresentationDeltaSeconds > 0f)
                {
                    m_Tick.FrameLateUpdate();
                    RefreshPreviewImage();
                }
                if (Session.LifecycleState == SimulationSessionLifecycleState.Failed)
                    throw new InvalidOperationException(Session.Failure.ToString());
                if (preparing && IsReady)
                    Changed?.Invoke();
            }
            catch (Exception exception)
            {
                Error = exception.Message;
                Close();
            }
        }

        internal void Close()
        {
            if (!IsOpen)
                return;
            EditorApplication.update -= Update;
            m_Cancellation.Cancel();
            Renderer.Reset();
            Session?.Stop();
            for (int i = 0; i < m_Actors.Length; i++)
                m_Actors[i].Release();
            if (m_Tick != null)
                GameplayTickSystem.Shutdown();
            m_Tick = null;
            m_Actors = Array.Empty<FixedCharacterHost>();
            Actor = null;
            Session = null;
            Profile = null;
            EditorSceneManager.ClosePreviewScene(m_Scene);
            m_Scene = default;
            m_Root = null;
            m_Cancellation.Dispose();
            m_Cancellation = null;
            Changed?.Invoke();
        }
    }
}
