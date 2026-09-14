using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using ThirdPersonCharacter.Editor.ProductStartup;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonGameplay.ScenePlay;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonCharacter.Editor.CharacterPipeline.Preview
{
    [InitializeOnLoad]
    public sealed class BtsmtlScenePlayPreviewCoordinator : IBtsmtlScenePlayPreviewOperations
    {
        const string LauncherOwnerId = "btsmtl-scene-play-preview";
        const string SceneGenerationKey = "ThirdPerson.BtsmtlScenePlay.SceneGeneration";
        const string ResumeAfterPreparationKey = "ThirdPerson.BtsmtlScenePlay.ResumeAfterPreparation";
        const string ResetWasPausedKey = "ThirdPerson.BtsmtlScenePlay.ResetWasPaused";
        const string StopRequestedKey = "ThirdPerson.BtsmtlScenePlay.StopRequested";
        const string StopFailureKey = "ThirdPerson.BtsmtlScenePlay.StopFailure";
        const string FailureOperationKey = "ThirdPerson.BtsmtlScenePlay.FailureOperation";
        const string FailureStageKey = "ThirdPerson.BtsmtlScenePlay.FailureStage";
        const string FailureCodeKey = "ThirdPerson.BtsmtlScenePlay.FailureCode";
        const string FailureMessageKey = "ThirdPerson.BtsmtlScenePlay.FailureMessage";
        const string TerminalFailureKey = "ThirdPerson.BtsmtlScenePlay.TerminalFailure";
        const string TerminalRequestIdKey = "ThirdPerson.BtsmtlScenePlay.Terminal.RequestId";
        const string TerminalScenePathKey = "ThirdPerson.BtsmtlScenePlay.Terminal.ScenePath";
        const string TerminalContextIdKey = "ThirdPerson.BtsmtlScenePlay.Terminal.ContextId";
        const string TerminalGenerationKey = "ThirdPerson.BtsmtlScenePlay.Terminal.Generation";
        const string TerminalOperationKey = "ThirdPerson.BtsmtlScenePlay.Terminal.Operation";
        const string TerminalStageKey = "ThirdPerson.BtsmtlScenePlay.Terminal.Stage";
        const string TerminalCodeKey = "ThirdPerson.BtsmtlScenePlay.Terminal.Code";
        const string TerminalMessageKey = "ThirdPerson.BtsmtlScenePlay.Terminal.Message";

        static readonly BtsmtlScenePlayPreviewCoordinator s_Instance =
            new BtsmtlScenePlayPreviewCoordinator();

        readonly HashSet<Guid> m_Interests = new HashSet<Guid>();
        BtsmtlScenePlayStatus m_Status = BtsmtlScenePlayStatus.Idle;
        BtsmtlScenePlayBuildStatus m_BuildStatus = BtsmtlScenePlayBuildStatus.Idle;
        BtsmtlScenePlayRequest m_Request;
        BtsmtlScenePlayContextDescriptor m_ContextDescriptor;
        readonly List<string> m_ActorIds = new List<string>();
        readonly List<BtsmtlScenePlaySkillOption> m_SkillOptions = new List<BtsmtlScenePlaySkillOption>();
        bool m_HasRequest;
        bool m_ResetWasPaused;
        bool m_ResumeAfterPreparation;
        bool m_BuildWasPaused;
        bool m_StopFailure;
        int m_PreparationPolls;
        double m_PreparationStartedAt;
        Task<CharacterSimulationBackgroundBuildResult> m_CharacterBuildTask;
        CharacterPipelineHost m_CharacterBuildHost;
        CharacterPipelineHost m_BuildAdoptionHost;
        string m_BuildActorId = string.Empty;
        string m_BuildSourceRevision = string.Empty;
        ulong m_BuildRequestedEpoch;
        double m_BuildStartedAt;
        double m_BuildPublishedAt;
        double m_BuildElapsedSeconds;
        double m_LastBuildTimingNotificationAt;

        static BtsmtlScenePlayPreviewCoordinator()
        {
            _ = s_Instance;
        }

        BtsmtlScenePlayPreviewCoordinator()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorPlayModeSceneLauncher.StateChanged -= OnLauncherStateChanged;
            EditorPlayModeSceneLauncher.StateChanged += OnLauncherStateChanged;
            BtsmtlScenePlayPreviewOperationsRegistry.Register(this);
            RestorePendingRequest();
        }

        public static BtsmtlScenePlayPreviewCoordinator Instance => s_Instance;
        public BtsmtlScenePlayStatus Status => m_Status;
        public BtsmtlScenePlayBuildStatus BuildStatus => m_BuildStatus;
        public bool SupportsInputReplay =>
            m_ContextDescriptor.HasCharacterRuntime &&
            m_ContextDescriptor.SessionHost.SupportsInputReplay;
        public bool SupportsPresentationCheckpointRestore =>
            m_ContextDescriptor.HasCharacterRuntime &&
            m_ContextDescriptor.SessionHost.SupportsPresentationCheckpointRestore;
        public bool IsInputRecording =>
            m_ContextDescriptor.HasCharacterRuntime &&
            m_ContextDescriptor.SessionHost.IsInputRecording;
        public IReadOnlyList<string> ActorIds => m_ActorIds;
        public IReadOnlyList<BtsmtlScenePlaySkillOption> SkillOptions => m_SkillOptions;
        public event Action<BtsmtlScenePlayStatus> StatusChanged;

        public bool TryQueueBackgroundCharacterBuild(
            CharacterPipelineHost host,
            out string error)
        {
            error = string.Empty;
            if (host == null)
            {
                error = "Background Character build requires a CharacterPipelineHost.";
                return false;
            }
            if (TryGetUnsavedAuthoringDependency(host.Definition, out string unsavedPath))
            {
                error = $"Authoring dependency '{unsavedPath}' has unsaved changes. Save the Character authoring assets before starting a Play build.";
                return false;
            }
            if (!EditorApplication.isPlaying ||
                (m_Status.State != BtsmtlScenePlayState.Running && m_Status.State != BtsmtlScenePlayState.Paused))
            {
                error = "Background Character build requires a running Scene Play Session.";
                return false;
            }
            if (m_CharacterBuildTask != null && !m_CharacterBuildTask.IsCompleted)
            {
                error = "A background Character build is already running.";
                return false;
            }
            try
            {
                m_CharacterBuildHost = host;
                m_CharacterBuildTask = CharacterSimulationBuildOrchestrator.StartBackgroundCharacter(
                    host.Definition,
                    CharacterSimulationTargetCatalog.DefaultEditor(host.Definition));
                return true;
            }
            catch (Exception exception)
            {
                m_CharacterBuildTask = null;
                m_CharacterBuildHost = null;
                error = exception.Message;
                return false;
            }
        }

        void RestorePendingRequest()
        {
            if (m_HasRequest || m_Status.HasFailure)
                return;
            if (!EditorPlayModeSceneLauncher.TryGetPendingRequest(
                    out EditorPlayModeSceneLaunchRequest launch) ||
                !string.Equals(launch.OwnerId, LauncherOwnerId, StringComparison.Ordinal))
            {
                RestoreTerminalStatus();
                return;
            }
            m_Request = new BtsmtlScenePlayRequest(
                launch.ScenePath,
                launch.ContextId,
                launch.RequestId,
                launch.StartPaused);
            m_HasRequest = true;
            m_StopFailure = SessionState.GetBool(StopFailureKey, false);
            m_ResetWasPaused = SessionState.GetBool(ResetWasPausedKey, false);
            m_ResumeAfterPreparation = SessionState.GetBool(
                ResumeAfterPreparationKey,
                launch.StartPaused);
            ulong sceneGeneration = ReadSceneGeneration();
            m_PreparationPolls = 0;
            m_PreparationStartedAt = EditorApplication.timeSinceStartup;
            if (m_StopFailure)
            {
                SetStatus(
                    BtsmtlScenePlayState.Faulted,
                    (BtsmtlScenePlayOperation)SessionState.GetInt(
                        FailureOperationKey,
                        (int)BtsmtlScenePlayOperation.Stop),
                    m_Request.Identity,
                    sceneGeneration,
                    (BtsmtlScenePlayFailureStage)SessionState.GetInt(
                        FailureStageKey,
                        (int)BtsmtlScenePlayFailureStage.Stop),
                    SessionState.GetString(FailureCodeKey, "scene_play_failed"),
                    SessionState.GetString(FailureMessageKey, "Scene Play preview failed."));
                return;
            }
            if (SessionState.GetBool(StopRequestedKey, false))
            {
                SetStatus(
                    BtsmtlScenePlayState.Stopping,
                    BtsmtlScenePlayOperation.Stop,
                    m_Request.Identity,
                    sceneGeneration,
                    BtsmtlScenePlayFailureStage.None,
                    string.Empty,
                    string.Empty);
                return;
            }
            SetStatus(
                EditorApplication.isPlaying
                    ? BtsmtlScenePlayState.Preparing
                    : BtsmtlScenePlayState.EnteringPlay,
                BtsmtlScenePlayOperation.Start,
                m_Request.Identity,
                sceneGeneration,
                BtsmtlScenePlayFailureStage.None,
                string.Empty,
                string.Empty);
        }

        void RestoreTerminalStatus()
        {
            if (!SessionState.GetBool(TerminalFailureKey, false))
                return;
            if (!Guid.TryParse(
                    SessionState.GetString(TerminalRequestIdKey, string.Empty),
                    out Guid requestId) ||
                requestId == Guid.Empty)
            {
                ClearTerminalStatus();
                return;
            }
            string scenePath = SessionState.GetString(TerminalScenePathKey, string.Empty);
            string contextId = SessionState.GetString(TerminalContextIdKey, string.Empty);
            if (string.IsNullOrWhiteSpace(scenePath) ||
                string.IsNullOrWhiteSpace(contextId))
            {
                ClearTerminalStatus();
                return;
            }
            scenePath = scenePath.Trim();
            contextId = contextId.Trim();
            BtsmtlScenePlayRequestIdentity identity = new BtsmtlScenePlayRequestIdentity(
                requestId,
                scenePath,
                contextId);
            ulong generation = ulong.TryParse(
                                   SessionState.GetString(TerminalGenerationKey, string.Empty),
                                   NumberStyles.None,
                                   CultureInfo.InvariantCulture,
                                   out ulong parsedGeneration) &&
                               parsedGeneration != 0
                ? parsedGeneration
                : 1;
            m_Status = new BtsmtlScenePlayStatus(
                BtsmtlScenePlayState.Faulted,
                (BtsmtlScenePlayOperation)SessionState.GetInt(
                    TerminalOperationKey,
                    (int)BtsmtlScenePlayOperation.Stop),
                identity,
                generation,
                (BtsmtlScenePlayFailureStage)SessionState.GetInt(
                    TerminalStageKey,
                    (int)BtsmtlScenePlayFailureStage.Request),
                SessionState.GetString(TerminalCodeKey, "scene_play_failed"),
                SessionState.GetString(TerminalMessageKey, "Scene Play preview failed."));
            StatusChanged?.Invoke(m_Status);
        }

        public BtsmtlScenePlayCommandResult Start(BtsmtlScenePlayRequest request)
        {
            if (request == null)
                return Rejected(
                    BtsmtlScenePlayOperation.Start,
                    BtsmtlScenePlayCommandResultCode.RejectedInvalidRequest,
                    "Scene Play preview request is missing.");
            if (m_HasRequest || EditorPlayModeSceneLauncher.IsPending)
                return Rejected(BtsmtlScenePlayOperation.Start, BtsmtlScenePlayCommandResultCode.RejectedBusy, "A Scene Play preview is already active.");
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return Rejected(BtsmtlScenePlayOperation.Start, BtsmtlScenePlayCommandResultCode.RejectedExternalPlay, "Unity is already running another Play session.");
            ClearTerminalStatus();
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(request.Identity.ScenePath))
                return RejectConfiguration(BtsmtlScenePlayOperation.Start, "preview_scene_missing", $"Preview Scene is missing: {request.Identity.ScenePath}", request.Identity, BtsmtlScenePlayFailureStage.Scene);
            if (!TryValidateContextBeforePlay(request.Identity, out string contextFailureCode, out string contextFailureMessage))
                return RejectConfiguration(
                    BtsmtlScenePlayOperation.Start,
                    contextFailureCode,
                    contextFailureMessage,
                    request.Identity,
                    contextFailureCode is "preview_character_authoring_unsaved" or
                    "preview_character_product_check_failed" or
                    "preview_character_build_required" or
                    "preview_character_product_mismatch"
                        ? BtsmtlScenePlayFailureStage.Product
                        : BtsmtlScenePlayFailureStage.Context);

            SetStatus(
                BtsmtlScenePlayState.Checking,
                BtsmtlScenePlayOperation.Start,
                request.Identity,
                0,
                BtsmtlScenePlayFailureStage.None,
                string.Empty,
                string.Empty);

            m_Request = request;
            m_ContextDescriptor = default;
            m_ActorIds.Clear();
            m_SkillOptions.Clear();
            m_HasRequest = true;
            m_StopFailure = false;
            m_ResetWasPaused = false;
            m_ResumeAfterPreparation = request.StartPaused;
            const ulong initialSceneGeneration = 1;
            SetStatus(
                BtsmtlScenePlayState.EnteringPlay,
                BtsmtlScenePlayOperation.Start,
                request.Identity,
                initialSceneGeneration,
                BtsmtlScenePlayFailureStage.None,
                string.Empty,
                string.Empty);
            PersistCoordinatorState(initialSceneGeneration);
            EditorPlayModeSceneLaunchResult launch = EditorPlayModeSceneLauncher.Start(
                new EditorPlayModeSceneLaunchRequest(
                    request.Identity.ScenePath,
                    request.Identity.ContextId,
                    request.Identity.RequestId,
                    prepare: request.Prepare,
                    startPaused: request.StartPaused,
                    ownerId: LauncherOwnerId));
            if (launch.Accepted)
                return Accepted(BtsmtlScenePlayOperation.Start);
            ClearActiveRequest();
            if (launch.Code == EditorPlayModeSceneLaunchResultCode.Cancelled)
            {
                SetStatus(
                    BtsmtlScenePlayState.Idle,
                    BtsmtlScenePlayOperation.Start,
                    request.Identity,
                    0,
                    BtsmtlScenePlayFailureStage.Request,
                    "launch_cancelled",
                    launch.Message);
                return new BtsmtlScenePlayCommandResult(
                    BtsmtlScenePlayCommandResultCode.Cancelled,
                    BtsmtlScenePlayOperation.Start,
                    m_Status,
                    launch.Message);
            }
            return RejectConfiguration(
                BtsmtlScenePlayOperation.Start,
                "scene_launch_failed",
                launch.Message,
                request.Identity,
                BtsmtlScenePlayFailureStage.EnterPlay);
        }

        public BtsmtlScenePlaySkillRequestResult RequestSkill(string actorId, string skillId)
        {
            if (m_BuildStatus.State == BtsmtlScenePlayBuildState.Building ||
                m_BuildStatus.State == BtsmtlScenePlayBuildState.Published)
                return RejectSkill(
                    BtsmtlScenePlaySkillRequestResultCode.RejectedProgramAdoptionPending,
                    actorId,
                    skillId,
                    string.Empty,
                    "Scene Play is waiting for the current Character Program adoption to complete before accepting a skill request.");
            if (m_Status.State != BtsmtlScenePlayState.Running)
                return RejectSkill(
                    BtsmtlScenePlaySkillRequestResultCode.RejectedNotRunning,
                    actorId,
                    skillId,
                    string.Empty,
                    "Scene Play must be Running before a skill request can be submitted.");
            if (!m_HasRequest || m_ContextDescriptor.Context == null)
                return RejectSkill(
                    BtsmtlScenePlaySkillRequestResultCode.RejectedActorMissing,
                    actorId,
                    skillId,
                    string.Empty,
                    "Scene Play has no connected Character Actor context.");

            BtsmtlScenePlaySkillOption option = default;
            int matchingSkillCount = 0;
            for (int i = 0; i < m_SkillOptions.Count; i++)
            {
                if (!string.Equals(m_SkillOptions[i].ActorId, actorId, StringComparison.Ordinal) ||
                    !string.Equals(m_SkillOptions[i].SkillId, skillId, StringComparison.Ordinal))
                    continue;
                option = m_SkillOptions[i];
                matchingSkillCount++;
            }
            if (matchingSkillCount == 0)
                return RejectSkill(
                    BtsmtlScenePlaySkillRequestResultCode.RejectedSkillMissing,
                    actorId,
                    skillId,
                    string.Empty,
                    $"Skill '{skillId}' is not declared by the selected Scene Play Actor.");
            if (matchingSkillCount > 1)
                return RejectSkill(
                    BtsmtlScenePlaySkillRequestResultCode.RejectedSkillAmbiguous,
                    actorId,
                    skillId,
                    string.Empty,
                    $"Skill '{skillId}' is declared more than once by the selected Scene Play Actor; the request cannot choose the first matching definition.");
            if (string.IsNullOrEmpty(option.SourceInputRequestId))
                return RejectSkill(
                    BtsmtlScenePlaySkillRequestResultCode.RejectedInputRequestMissing,
                    actorId,
                    skillId,
                    string.Empty,
                    $"Skill '{skillId}' has no formal source input request.");
            int matchingInputRequestCount = 0;
            for (int i = 0; i < m_SkillOptions.Count; i++)
            {
                if (string.Equals(m_SkillOptions[i].ActorId, actorId, StringComparison.Ordinal) &&
                    string.Equals(m_SkillOptions[i].SourceInputRequestId, option.SourceInputRequestId, StringComparison.Ordinal))
                    matchingInputRequestCount++;
            }
            if (matchingInputRequestCount > 1)
                return RejectSkill(
                    BtsmtlScenePlaySkillRequestResultCode.RejectedSkillAmbiguous,
                    actorId,
                    skillId,
                    option.SourceInputRequestId,
                    $"Skill '{skillId}' shares input request '{option.SourceInputRequestId}' with other skills; the formal Control Module must select the candidate from its declared control inputs.");

            for (int i = 0; i < m_ContextDescriptor.Actors.Count; i++)
            {
                BtsmtlScenePlayActorDescriptor actor = m_ContextDescriptor.Actors[i];
                if (!string.Equals(actor.ActorId.Value, actorId, StringComparison.Ordinal))
                    continue;
                if (!TryBeginDiagnosticsCapture(
                        actor.Host,
                        RuntimeDiagnosticsCaptureDetail.Continuous,
                        out string captureError))
                {
                    return RejectSkill(
                        BtsmtlScenePlaySkillRequestResultCode.RejectedSourceUnavailable,
                        actorId,
                        skillId,
                        option.SourceInputRequestId,
                        captureError);
                }
                if (!m_ContextDescriptor.SessionHost.TryCaptureCheckpointNow(
                        out ulong checkpointTick,
                        out string checkpointError))
                {
                    return RejectSkill(
                        BtsmtlScenePlaySkillRequestResultCode.RejectedSourceUnavailable,
                        actorId,
                        skillId,
                        option.SourceInputRequestId,
                        checkpointError);
                }
                if (!actor.Host.TryQueueInputRequest(
                        option.SourceInputRequestId,
                        out ulong requestSequence,
                        out string error))
                {
                    return RejectSkill(
                        BtsmtlScenePlaySkillRequestResultCode.RejectedSourceUnavailable,
                        actorId,
                        skillId,
                        option.SourceInputRequestId,
                        string.IsNullOrEmpty(error)
                            ? "The formal Character Control Source rejected the input request."
                            : error);
                }
                return new BtsmtlScenePlaySkillRequestResult(
                    BtsmtlScenePlaySkillRequestResultCode.Accepted,
                    actorId,
                    skillId,
                     option.SourceInputRequestId,
                     requestSequence,
                     m_Status.SceneGeneration,
                     "The formal Character Control Source queued the skill input request.",
                    m_ContextDescriptor.SessionHost.ExecutionBranchId,
                    checkpointTick);
            }
            return RejectSkill(
                BtsmtlScenePlaySkillRequestResultCode.RejectedActorMissing,
                actorId,
                skillId,
                option.SourceInputRequestId,
                "The requested Actor is no longer present in the connected Scene Play context.");
        }

        public BtsmtlScenePlayCommandResult Pause()
        {
            if (m_Status.State != BtsmtlScenePlayState.Running)
                return Rejected(BtsmtlScenePlayOperation.Pause, BtsmtlScenePlayCommandResultCode.RejectedNotRunning, "Scene Play preview is not running.");
            EditorApplication.isPaused = true;
            SetStatus(
                BtsmtlScenePlayState.Paused,
                BtsmtlScenePlayOperation.Pause,
                m_Request.Identity,
                m_Status.SceneGeneration,
                BtsmtlScenePlayFailureStage.None,
                string.Empty,
                string.Empty);
            return Accepted(BtsmtlScenePlayOperation.Pause);
        }

        public BtsmtlScenePlayCommandResult Resume()
        {
            if (m_Status.State != BtsmtlScenePlayState.Paused)
                return Rejected(BtsmtlScenePlayOperation.Resume, BtsmtlScenePlayCommandResultCode.RejectedNotRunning, "Scene Play preview is not paused.");
            EditorApplication.isPaused = false;
            SetStatus(
                BtsmtlScenePlayState.Running,
                BtsmtlScenePlayOperation.Resume,
                m_Request.Identity,
                m_Status.SceneGeneration,
                BtsmtlScenePlayFailureStage.None,
                string.Empty,
                string.Empty);
            return Accepted(BtsmtlScenePlayOperation.Resume);
        }

        public BtsmtlScenePlayCommandResult Reset()
        {
            if (m_Status.State != BtsmtlScenePlayState.Running &&
                m_Status.State != BtsmtlScenePlayState.Paused)
                return Rejected(BtsmtlScenePlayOperation.Reset, BtsmtlScenePlayCommandResultCode.RejectedNotRunning, "Scene Play preview is not running.");
            m_ResetWasPaused = m_Status.State == BtsmtlScenePlayState.Paused;
            m_ResumeAfterPreparation = m_ResetWasPaused;
            SetStatus(
                BtsmtlScenePlayState.Resetting,
                BtsmtlScenePlayOperation.Reset,
                m_Request.Identity,
                m_Status.SceneGeneration,
                BtsmtlScenePlayFailureStage.None,
                string.Empty,
                string.Empty);
            PersistCoordinatorState(m_Status.SceneGeneration);
            EditorApplication.isPaused = false;
            try
            {
                if (!m_ContextDescriptor.Context.TryReleaseRuntime(
                        out BtsmtlScenePlayContextDiagnostic diagnostic))
                {
                    return FailAndStop(
                        BtsmtlScenePlayOperation.Reset,
                        BtsmtlScenePlayFailureStage.Reset,
                        "preview_runtime_release_failed",
                        diagnostic.Message);
                }
                EditorPlayModeSceneReloadResult reload =
                    EditorPlayModeSceneLauncher.ReloadInPlayMode(
                        m_Request.Identity.RequestId,
                        LauncherOwnerId);
                if (!reload.Accepted)
                    return FailAndStop(
                        BtsmtlScenePlayOperation.Reset,
                        BtsmtlScenePlayFailureStage.Reset,
                        "preview_scene_reset_failed",
                        reload.Message);
                reload.Operation.completed += OnResetSceneLoaded;
                return Accepted(BtsmtlScenePlayOperation.Reset);
            }
            catch (Exception exception)
            {
                return FailAndStop(
                    BtsmtlScenePlayOperation.Reset,
                    BtsmtlScenePlayFailureStage.Reset,
                    "preview_scene_reset_failed",
                    exception.Message);
            }
        }

        public BtsmtlScenePlayCommandResult Stop()
        {
            if (!m_HasRequest)
                return Rejected(BtsmtlScenePlayOperation.Stop, BtsmtlScenePlayCommandResultCode.RejectedNotRunning, "Scene Play preview is not active.");
            SetStatus(
                BtsmtlScenePlayState.Stopping,
                BtsmtlScenePlayOperation.Stop,
                m_Request.Identity,
                m_Status.SceneGeneration,
                BtsmtlScenePlayFailureStage.None,
                string.Empty,
                string.Empty);
            PersistCoordinatorState(m_Status.SceneGeneration);
            EditorApplication.isPaused = false;
            try
            {
                if (m_ContextDescriptor.Context)
                {
                    if (!m_ContextDescriptor.Context.TryReleaseRuntime(
                            out BtsmtlScenePlayContextDiagnostic diagnostic))
                    {
                        throw new InvalidOperationException(diagnostic.Message);
                    }
                }
            }
            catch (Exception exception)
            {
                m_StopFailure = true;
                SetStatus(
                    BtsmtlScenePlayState.Faulted,
                    BtsmtlScenePlayOperation.Stop,
                    m_Request.Identity,
                    m_Status.SceneGeneration,
                    BtsmtlScenePlayFailureStage.Stop,
                    "preview_stop_failed",
                    exception.Message);
                PersistFailureState(m_Status);
            }
            EditorApplication.isPlaying = false;
            return m_StopFailure
                ? new BtsmtlScenePlayCommandResult(
                    BtsmtlScenePlayCommandResultCode.Failed,
                    BtsmtlScenePlayOperation.Stop,
                    m_Status,
                    m_Status.FailureMessage)
                : Accepted(BtsmtlScenePlayOperation.Stop);
        }

        public BtsmtlScenePlayCommandResult Build(string actorId)
        {
            if (m_Status.State != BtsmtlScenePlayState.Running &&
                m_Status.State != BtsmtlScenePlayState.Paused)
                return Rejected(
                    BtsmtlScenePlayOperation.Build,
                    BtsmtlScenePlayCommandResultCode.RejectedNotRunning,
                    "Scene Play preview must be running or paused before a Character build.");
            if (string.IsNullOrWhiteSpace(actorId))
                return Rejected(
                    BtsmtlScenePlayOperation.Build,
                    BtsmtlScenePlayCommandResultCode.RejectedInvalidRequest,
                    "Character build requires an explicit ActorId.");
            if (!m_HasRequest || m_ContextDescriptor.Context == null)
                return Rejected(
                    BtsmtlScenePlayOperation.Build,
                    BtsmtlScenePlayCommandResultCode.RejectedConfiguration,
                    "Scene Play has no connected Character Actor context.");

            CharacterPipelineHost host = null;
            string requestedActorId = actorId.Trim();
            for (int i = 0; i < m_ContextDescriptor.Actors.Count; i++)
            {
                BtsmtlScenePlayActorDescriptor actor = m_ContextDescriptor.Actors[i];
                if (!string.Equals(actor.ActorId.Value, requestedActorId, StringComparison.Ordinal))
                    continue;
                if (host != null)
                    return Rejected(
                        BtsmtlScenePlayOperation.Build,
                        BtsmtlScenePlayCommandResultCode.RejectedConfiguration,
                        $"ActorId '{requestedActorId}' is ambiguous in the Scene Play context.");
                host = actor.Host;
            }
            if (host == null)
                return Rejected(
                    BtsmtlScenePlayOperation.Build,
                    BtsmtlScenePlayCommandResultCode.RejectedConfiguration,
                    $"ActorId '{requestedActorId}' is not present in the connected Scene Play context.");
            if (m_CharacterBuildTask != null && !m_CharacterBuildTask.IsCompleted)
                return Rejected(
                    BtsmtlScenePlayOperation.Build,
                    BtsmtlScenePlayCommandResultCode.RejectedBusy,
                    "A background Character build is already running.");
            if (m_BuildStatus.State == BtsmtlScenePlayBuildState.Published)
                return Rejected(
                    BtsmtlScenePlayOperation.Build,
                    BtsmtlScenePlayCommandResultCode.RejectedBusy,
                    "The previous Character build is waiting for Session adoption at a Logic Tick boundary.");
            m_BuildStartedAt = EditorApplication.timeSinceStartup;
            if (!TryQueueBackgroundCharacterBuild(host, out string error))
            {
                m_BuildStartedAt = 0d;
                return Rejected(
                    BtsmtlScenePlayOperation.Build,
                    BtsmtlScenePlayCommandResultCode.RejectedConfiguration,
                    error);
            }
            m_BuildWasPaused = m_Status.State == BtsmtlScenePlayState.Paused;
            m_BuildAdoptionHost = host;
            m_BuildActorId = requestedActorId;
            m_BuildSourceRevision = string.Empty;
            m_BuildRequestedEpoch = 0;
            m_BuildPublishedAt = 0d;
            m_BuildElapsedSeconds = -1d;
            m_LastBuildTimingNotificationAt = 0d;
            m_BuildStatus = new BtsmtlScenePlayBuildStatus(
                BtsmtlScenePlayBuildState.Building,
                m_BuildActorId,
                null,
                "Background Character build is running.");
            SetStatus(
                BtsmtlScenePlayState.Building,
                BtsmtlScenePlayOperation.Build,
                m_Request.Identity,
                m_Status.SceneGeneration,
                BtsmtlScenePlayFailureStage.None,
                string.Empty,
                string.Empty);
            PersistCoordinatorState(m_Status.SceneGeneration);
            return new BtsmtlScenePlayCommandResult(
                BtsmtlScenePlayCommandResultCode.Accepted,
                BtsmtlScenePlayOperation.Build,
                m_Status,
                "Background Character build started; the current Program and Projection remain active until adoption at the next Logic Tick boundary.");
        }

        public BtsmtlScenePlayCommandResult StartInputRecording()
        {
            if (m_Status.State != BtsmtlScenePlayState.Running &&
                m_Status.State != BtsmtlScenePlayState.Paused)
                return Rejected(
                    BtsmtlScenePlayOperation.StartInputRecording,
                    BtsmtlScenePlayCommandResultCode.RejectedNotRunning,
                    "Scene Play preview must be running or paused before input recording can start.");
            if (!m_ContextDescriptor.HasCharacterRuntime)
                return Rejected(
                    BtsmtlScenePlayOperation.StartInputRecording,
                    BtsmtlScenePlayCommandResultCode.RejectedConfiguration,
                    "Scene Play has no Character Session input recording runtime.");
            if (!m_ContextDescriptor.SessionHost.TryStartInputRecording(out string error))
                return Rejected(
                    BtsmtlScenePlayOperation.StartInputRecording,
                    BtsmtlScenePlayCommandResultCode.RejectedConfiguration,
                    error);
            return new BtsmtlScenePlayCommandResult(
                BtsmtlScenePlayCommandResultCode.Accepted,
                BtsmtlScenePlayOperation.StartInputRecording,
                m_Status,
                "Formal Character input recording started.");
        }

        public BtsmtlScenePlayCommandResult StopInputRecording()
        {
            if (m_Status.State != BtsmtlScenePlayState.Running &&
                m_Status.State != BtsmtlScenePlayState.Paused)
                return Rejected(
                    BtsmtlScenePlayOperation.StopInputRecording,
                    BtsmtlScenePlayCommandResultCode.RejectedNotRunning,
                    "Scene Play preview must be running or paused before input recording can stop.");
            if (!m_ContextDescriptor.HasCharacterRuntime)
                return Rejected(
                    BtsmtlScenePlayOperation.StopInputRecording,
                    BtsmtlScenePlayCommandResultCode.RejectedConfiguration,
                    "Scene Play has no Character Session input recording runtime.");
            if (!m_ContextDescriptor.SessionHost.TryStopInputRecording(out string error))
                return Rejected(
                    BtsmtlScenePlayOperation.StopInputRecording,
                    BtsmtlScenePlayCommandResultCode.RejectedConfiguration,
                    error);
            return new BtsmtlScenePlayCommandResult(
                BtsmtlScenePlayCommandResultCode.Accepted,
                BtsmtlScenePlayOperation.StopInputRecording,
                m_Status,
                "Formal Character input recording stopped and is available for Session replay.");
        }

        public BtsmtlScenePlayCommandResult ResumeFromTick(ulong tick)
        {
            if (m_Status.State != BtsmtlScenePlayState.Running &&
                m_Status.State != BtsmtlScenePlayState.Paused)
                return Rejected(
                    BtsmtlScenePlayOperation.Restore,
                    BtsmtlScenePlayCommandResultCode.RejectedNotRunning,
                    "Scene Play preview must be running or paused before checkpoint restore.");
            if (!m_ContextDescriptor.HasCharacterRuntime)
                return Rejected(
                    BtsmtlScenePlayOperation.Restore,
                    BtsmtlScenePlayCommandResultCode.RejectedConfiguration,
                    "Scene Play has no Character Session checkpoint runtime.");
            if (!m_ContextDescriptor.SessionHost.TryRestoreToTick(
                    tick,
                    out ulong checkpointTick,
                    out string error))
                return Rejected(
                    BtsmtlScenePlayOperation.Restore,
                    BtsmtlScenePlayCommandResultCode.RejectedConfiguration,
                    error);
            EditorApplication.isPaused = false;
            SetStatus(
                BtsmtlScenePlayState.Running,
                BtsmtlScenePlayOperation.Restore,
                m_Request.Identity,
                m_Status.SceneGeneration,
                BtsmtlScenePlayFailureStage.None,
                string.Empty,
                string.Empty);
            return new BtsmtlScenePlayCommandResult(
                BtsmtlScenePlayCommandResultCode.Accepted,
                BtsmtlScenePlayOperation.Restore,
                m_Status,
                checkpointTick == tick
                    ? $"Session restored from checkpoint Tick {tick} and continued on a new ExecutionBranch."
                    : $"Session restored from checkpoint Tick {checkpointTick} and queued formal input replay through target Tick {tick} on a new ExecutionBranch.");
        }

        public BtsmtlScenePlayCommandResult ReplayInputRange(ulong fromTick, ulong toTick)
        {
            if (m_Status.State != BtsmtlScenePlayState.Running &&
                m_Status.State != BtsmtlScenePlayState.Paused)
                return Rejected(
                    BtsmtlScenePlayOperation.Replay,
                    BtsmtlScenePlayCommandResultCode.RejectedNotRunning,
                    "Scene Play preview must be running or paused before input replay.");
            if (fromTick >= toTick)
                return Rejected(
                    BtsmtlScenePlayOperation.Replay,
                    BtsmtlScenePlayCommandResultCode.RejectedInvalidRequest,
                    "Input replay requires a non-empty increasing Tick range.");
            if (!m_ContextDescriptor.HasCharacterRuntime)
                return Rejected(
                    BtsmtlScenePlayOperation.Replay,
                    BtsmtlScenePlayCommandResultCode.RejectedConfiguration,
                    "Scene Play has no Character Session input replay runtime.");
            if (!m_ContextDescriptor.SessionHost.TryReplayInputRange(fromTick, toTick, out string error))
                return Rejected(
                    BtsmtlScenePlayOperation.Replay,
                    BtsmtlScenePlayCommandResultCode.RejectedConfiguration,
                    error);
            EditorApplication.isPaused = false;
            SetStatus(
                BtsmtlScenePlayState.Running,
                BtsmtlScenePlayOperation.Replay,
                m_Request.Identity,
                m_Status.SceneGeneration,
                BtsmtlScenePlayFailureStage.None,
                string.Empty,
                string.Empty);
            return new BtsmtlScenePlayCommandResult(
                BtsmtlScenePlayCommandResultCode.Accepted,
                BtsmtlScenePlayOperation.Replay,
                m_Status,
                $"Session replay prepared from Tick {fromTick} to Tick {toTick} on a new ExecutionBranch.");
        }

        public void AddInterest(Guid ownerId)
        {
            if (ownerId == Guid.Empty)
                throw new ArgumentException("Scene Play interest owner identity is required.", nameof(ownerId));
            m_Interests.Add(ownerId);
        }

        public void RemoveInterest(Guid ownerId)
        {
            if (ownerId != Guid.Empty)
                m_Interests.Remove(ownerId);
        }

        void OnEditorUpdate()
        {
            PollBackgroundCharacterBuild();
            PollProgramAdoption();
            RefreshBuildTimingStatus();
            RestorePendingRequest();
            if (!m_HasRequest)
                return;
            if (TryHandleLostRequest())
                return;
            if ((m_Status.State == BtsmtlScenePlayState.Running ||
                 m_Status.State == BtsmtlScenePlayState.Paused ||
                 m_Status.State == BtsmtlScenePlayState.Building) &&
                TryHandleRuntimeFault())
                return;
            if (m_Status.State == BtsmtlScenePlayState.Building &&
                m_BuildWasPaused &&
                !EditorApplication.isPaused)
                EditorApplication.isPaused = true;
            if (m_Status.State == BtsmtlScenePlayState.Running && EditorApplication.isPaused)
            {
                SetStatus(
                    BtsmtlScenePlayState.Paused,
                    BtsmtlScenePlayOperation.Pause,
                    m_Request.Identity,
                    m_Status.SceneGeneration,
                    BtsmtlScenePlayFailureStage.None,
                    string.Empty,
                    string.Empty);
                return;
            }
            if (m_Status.State == BtsmtlScenePlayState.Paused && !EditorApplication.isPaused)
            {
                SetStatus(
                    BtsmtlScenePlayState.Running,
                    BtsmtlScenePlayOperation.Resume,
                    m_Request.Identity,
                    m_Status.SceneGeneration,
                    BtsmtlScenePlayFailureStage.None,
                    string.Empty,
                    string.Empty);
            }
            if (m_Status.State != BtsmtlScenePlayState.Preparing &&
                m_Status.State != BtsmtlScenePlayState.EnteringPlay)
                return;
            if (!EditorApplication.isPlaying)
                return;
            if (TryConnectContext())
                return;
            m_PreparationPolls++;
            if (m_PreparationPolls < 300 || EditorApplication.timeSinceStartup - m_PreparationStartedAt < 5d)
                return;
            FailAndStop(
                BtsmtlScenePlayOperation.Start,
                BtsmtlScenePlayFailureStage.Target,
                "preview_context_connection_timeout",
                "Scene Play context did not register a ready formal Runtime Owner.");
        }

        void PollBackgroundCharacterBuild()
        {
            if (m_CharacterBuildTask == null || !m_CharacterBuildTask.IsCompleted)
                return;
            Task<CharacterSimulationBackgroundBuildResult> task = m_CharacterBuildTask;
            CharacterPipelineHost host = m_CharacterBuildHost;
            m_CharacterBuildTask = null;
            m_CharacterBuildHost = null;
            if (!m_HasRequest || m_Status.State != BtsmtlScenePlayState.Building)
                return;
            try
            {
                CharacterSimulationBackgroundBuildResult result = task.GetAwaiter().GetResult();
                if (!result.IsValid)
                {
                    SetBuildFailure(BuildReportMessage(result), null);
                    RestoreAfterBuild(
                        "preview_program_build_failed",
                        BuildReportMessage(result),
                        true);
                    return;
                }
                if (!CharacterSimulationBuildOrchestrator.PublishBackgroundAndQueueAdoption(result, host, out string error))
                {
                    SimulationProgramAdoptionResult adoption =
                        m_ContextDescriptor.SessionHost.LastProgramAdoption;
                    if (adoption == null ||
                        !string.Equals(
                            adoption.Requested.SourceRevision.Value,
                            result.Artifact.Header.SourceRevision.Value,
                            StringComparison.Ordinal))
                    {
                        adoption = null;
                    }
                    SetBuildFailure(
                        string.IsNullOrEmpty(error) ? "Program publication or adoption failed." : error,
                        BuildTargetStatuses(result),
                        adoption);
                    RestoreAfterBuild(
                        "preview_program_adoption_failed",
                        string.IsNullOrEmpty(error) ? "Program publication or adoption failed." : error,
                        true);
                    return;
                }
                SimulationProgramAdoptionResult queuedAdoption = m_ContextDescriptor.SessionHost.LastProgramAdoption;
                if (queuedAdoption == null || queuedAdoption.Requested.Value == 0)
                {
                    SetBuildFailure(
                        "Session did not return a Program adoption request for the completed Build.",
                        BuildTargetStatuses(result));
                    RestoreAfterBuild(
                        "preview_program_adoption_failed",
                        "Session did not return a Program adoption request for the completed Build.",
                        true);
                    return;
                }
                m_BuildRequestedEpoch = queuedAdoption.Requested.Value;
                m_BuildSourceRevision = result.Artifact.Header.SourceRevision.Value;
                m_BuildElapsedSeconds = BuildElapsedSeconds();
                m_BuildPublishedAt = EditorApplication.timeSinceStartup;
                m_BuildStatus = new BtsmtlScenePlayBuildStatus(
                    BtsmtlScenePlayBuildState.Published,
                    m_BuildActorId,
                    BuildTargetStatuses(result),
                    "Program and Projection published; waiting for Session adoption at the next Logic Tick boundary.",
                    m_BuildRequestedEpoch,
                    0,
                    BuildAdoptionReport(queuedAdoption),
                    m_BuildElapsedSeconds,
                    0d);
                RestoreAfterBuild(
                    string.Empty,
                    "Background Program and Projection published; adoption is queued for the next Logic Tick boundary.",
                    false);
            }
            catch (Exception exception)
            {
                SetBuildFailure(exception.Message, null);
                RestoreAfterBuild("preview_program_build_failed", exception.Message, true);
            }
        }

        void PollProgramAdoption()
        {
            if (m_BuildStatus.State != BtsmtlScenePlayBuildState.Published ||
                !m_HasRequest ||
                m_BuildAdoptionHost == null ||
                !m_ContextDescriptor.HasCharacterRuntime ||
                (m_Status.State != BtsmtlScenePlayState.Running &&
                 m_Status.State != BtsmtlScenePlayState.Paused))
                return;
            SimulationProgramAdoptionResult adoption = m_ContextDescriptor.SessionHost.LastProgramAdoption;
            if (adoption == null ||
                adoption.Requested.Value != m_BuildRequestedEpoch ||
                !string.Equals(adoption.Requested.SourceRevision.Value, m_BuildSourceRevision, StringComparison.Ordinal))
                return;
            if (adoption.IsApplied)
            {
                BuildSkillOptions(m_ContextDescriptor);
                m_BuildStatus = new BtsmtlScenePlayBuildStatus(
                    BtsmtlScenePlayBuildState.Adopted,
                    m_BuildActorId,
                    m_BuildStatus.Targets,
                    $"Program Epoch {adoption.Current.Value} was adopted by the Session.",
                    m_BuildRequestedEpoch,
                    adoption.Current.Value,
                    BuildAdoptionReport(adoption),
                    m_BuildElapsedSeconds,
                    AdoptionWaitElapsedSeconds());
                m_BuildAdoptionHost = null;
                StatusChanged?.Invoke(m_Status);
                return;
            }
            if (adoption.Status == SimulationProgramAdoptionStatus.Rejected)
            {
                SetBuildFailure(adoption.Message, m_BuildStatus.Targets, adoption);
                SetStatus(
                    m_Status.State,
                    BtsmtlScenePlayOperation.Build,
                    m_Request.Identity,
                    m_Status.SceneGeneration,
                    BtsmtlScenePlayFailureStage.Product,
                    adoption.Code,
                    adoption.Message);
                m_BuildAdoptionHost = null;
            }
        }

        void RefreshBuildTimingStatus()
        {
            if (!m_HasRequest ||
                m_BuildStatus.State != BtsmtlScenePlayBuildState.Building &&
                m_BuildStatus.State != BtsmtlScenePlayBuildState.Published)
                return;
            double now = EditorApplication.timeSinceStartup;
            if (now - m_LastBuildTimingNotificationAt < 0.25d)
                return;
            double buildElapsed = m_BuildStatus.State == BtsmtlScenePlayBuildState.Building
                ? BuildElapsedSeconds()
                : m_BuildStatus.BuildElapsedSeconds;
            double adoptionWait = m_BuildStatus.State == BtsmtlScenePlayBuildState.Published
                ? AdoptionWaitElapsedSeconds()
                : 0d;
            m_BuildStatus = new BtsmtlScenePlayBuildStatus(
                m_BuildStatus.State,
                m_BuildStatus.ActorId,
                m_BuildStatus.Targets,
                m_BuildStatus.Message,
                m_BuildStatus.RequestedProgramEpoch,
                m_BuildStatus.AdoptedProgramEpoch,
                m_BuildStatus.Adoption,
                buildElapsed,
                adoptionWait);
            m_LastBuildTimingNotificationAt = now;
            StatusChanged?.Invoke(m_Status);
        }

        void RestoreAfterBuild(string failureCode, string message, bool failed)
        {
            if (!m_HasRequest || m_Status.State != BtsmtlScenePlayState.Building)
                return;
            BtsmtlScenePlayState state = m_BuildWasPaused
                ? BtsmtlScenePlayState.Paused
                : BtsmtlScenePlayState.Running;
            SetStatus(
                state,
                BtsmtlScenePlayOperation.Build,
                m_Request.Identity,
                m_Status.SceneGeneration,
                failed ? BtsmtlScenePlayFailureStage.Product : BtsmtlScenePlayFailureStage.None,
                failureCode,
                message);
            m_BuildWasPaused = false;
            PersistCoordinatorState(m_Status.SceneGeneration);
        }

        void SetBuildFailure(
            string message,
            IReadOnlyList<BtsmtlScenePlayBuildTargetStatus> targets,
            SimulationProgramAdoptionResult adoption = null)
        {
            m_BuildStatus = new BtsmtlScenePlayBuildStatus(
                BtsmtlScenePlayBuildState.Failed,
                m_BuildActorId,
                targets,
                message,
                m_BuildRequestedEpoch,
                adoption?.IsApplied == true ? adoption.Current.Value : 0,
                BuildAdoptionReport(adoption),
                BuildElapsedSeconds(),
                AdoptionWaitElapsedSeconds());
            m_BuildAdoptionHost = null;
        }

        double BuildElapsedSeconds()
        {
            if (m_BuildElapsedSeconds >= 0d)
                return m_BuildElapsedSeconds;
            if (m_BuildStartedAt <= 0d)
                return 0d;
            return Math.Max(0d, EditorApplication.timeSinceStartup - m_BuildStartedAt);
        }

        double AdoptionWaitElapsedSeconds()
        {
            if (m_BuildPublishedAt <= 0d)
                return 0d;
            return Math.Max(0d, EditorApplication.timeSinceStartup - m_BuildPublishedAt);
        }

        static BtsmtlScenePlayProgramAdoptionReport BuildAdoptionReport(
            SimulationProgramAdoptionResult adoption)
        {
            if (adoption == null)
                return null;
            return new BtsmtlScenePlayProgramAdoptionReport(
                (BtsmtlScenePlayProgramAdoptionStatus)adoption.Status,
                adoption.Current.Value,
                adoption.Current.SourceRevision.Value,
                adoption.Current.ProgramCatalogHash.ToString(),
                adoption.Requested.Value,
                adoption.Requested.SourceRevision.Value,
                adoption.Requested.ProgramCatalogHash.ToString(),
                adoption.Code,
                adoption.Message);
        }

        static IReadOnlyList<BtsmtlScenePlayBuildTargetStatus> BuildTargetStatuses(
            CharacterSimulationBackgroundBuildResult result)
        {
            if (result == null || result.TargetIdentities == null)
                return Array.Empty<BtsmtlScenePlayBuildTargetStatus>();
            var values = new List<BtsmtlScenePlayBuildTargetStatus>(result.TargetIdentities.Count);
            for (int i = 0; i < result.TargetIdentities.Count; i++)
            {
                CharacterSimulationBackgroundBuildTargetIdentity identity = result.TargetIdentities[i];
                values.Add(new BtsmtlScenePlayBuildTargetStatus(
                    identity.NumericProfileId.Value,
                    identity.ProgramId.Value,
                    identity.SourceRevision.Value,
                    identity.SemanticHash.Value.ToString(),
                    identity.ProgramHash.ToString(),
                    identity.LayoutHash.ToString(),
                    identity.SourceMapEntryCount,
                    identity.PresentationContractHash.ToString(),
                    identity.PresentationProjectionRevision));
            }
            return values.AsReadOnly();
        }

        static string BuildReportMessage(CharacterSimulationBackgroundBuildResult result)
        {
            if (result?.Report != null)
            {
                for (int i = 0; i < result.Report.Messages.Count; i++)
                {
                    CharacterSimulationCompileMessage message = result.Report.Messages[i];
                    if (message.Severity == CharacterSimulationCompileSeverity.Error)
                        return message.ToString();
                }
            }
            return "Background Character build did not produce a valid result.";
        }

        bool TryHandleLostRequest()
        {
            bool active =
                m_Status.State == BtsmtlScenePlayState.EnteringPlay ||
                m_Status.State == BtsmtlScenePlayState.Preparing ||
                m_Status.State == BtsmtlScenePlayState.Running ||
                m_Status.State == BtsmtlScenePlayState.Paused ||
                m_Status.State == BtsmtlScenePlayState.Building ||
                m_Status.State == BtsmtlScenePlayState.Resetting ||
                m_Status.State == BtsmtlScenePlayState.Stopping;
            if (!active || EditorPlayModeSceneLauncher.IsPending ||
                (m_Status.State == BtsmtlScenePlayState.Stopping &&
                 EditorApplication.isPlayingOrWillChangePlaymode))
                return false;
            string message = "The Scene Play request was lost before its lifecycle completed.";
            if (m_ContextDescriptor.Context &&
                !m_ContextDescriptor.Context.TryReleaseRuntime(
                    out BtsmtlScenePlayContextDiagnostic diagnostic))
            {
                message = $"{message} Runtime release failed: {diagnostic.Message}";
            }
            EditorApplication.isPaused = false;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.isPlaying = false;
            SetStatus(
                BtsmtlScenePlayState.Faulted,
                m_Status.Operation,
                m_Request.Identity,
                m_Status.SceneGeneration,
                BtsmtlScenePlayFailureStage.Request,
                "preview_request_lost",
                message);
            PersistTerminalStatus(m_Status);
            ClearActiveRequest();
            return true;
        }

        bool TryHandleRuntimeFault()
        {
            if (m_ContextDescriptor.HasContentRuntime)
            {
                BtsmtlScenePlayRuntimeOwnerStatus status =
                    m_ContextDescriptor.RuntimeOwner.Status;
                if (status.HasFailure)
                {
                    FailAndStop(
                        m_Status.Operation,
                        BtsmtlScenePlayFailureStage.Target,
                        string.IsNullOrEmpty(status.FailureCode)
                            ? "runtime_owner_failed"
                            : status.FailureCode,
                        string.IsNullOrEmpty(status.FailureMessage)
                            ? "Formal Runtime Owner failed while the preview was running."
                            : status.FailureMessage);
                    return true;
                }
                if (!status.IsReady)
                {
                    FailAndStop(
                        m_Status.Operation,
                        BtsmtlScenePlayFailureStage.Target,
                        "runtime_owner_not_ready",
                        "Formal Runtime Owner stopped being ready while the preview was running.");
                    return true;
                }
            }
            if (!m_ContextDescriptor.HasCharacterRuntime)
                return false;
            if (m_ContextDescriptor.SessionHost.LifecycleState == SimulationSessionLifecycleState.Failed)
            {
                SimulationSessionFailure failure = m_ContextDescriptor.SessionHost.Failure;
                FailAndStop(
                    m_Status.Operation,
                    BtsmtlScenePlayFailureStage.Target,
                    failure?.Code ?? "session_runtime_failed",
                    failure?.Message ?? "Formal Character Session failed while the preview was running.");
                return true;
            }
            if (m_ContextDescriptor.SessionHost.LifecycleState != SimulationSessionLifecycleState.Active)
            {
                FailAndStop(
                    m_Status.Operation,
                    BtsmtlScenePlayFailureStage.Target,
                    "session_runtime_not_active",
                    "Formal Character Session stopped being active while the preview was running.");
                return true;
            }
            return false;
        }

        void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!m_HasRequest)
                return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                if (m_StopFailure || m_Status.State == BtsmtlScenePlayState.Stopping)
                    return;
                m_PreparationPolls = 0;
                m_PreparationStartedAt = EditorApplication.timeSinceStartup;
                SetStatus(
                    BtsmtlScenePlayState.Preparing,
                    m_Status.Operation,
                    m_Request.Identity,
                    m_Status.SceneGeneration,
                    BtsmtlScenePlayFailureStage.None,
                    string.Empty,
                    string.Empty);
            }
            else if (state == PlayModeStateChange.ExitingPlayMode &&
                     m_Status.State != BtsmtlScenePlayState.Stopping &&
                     m_Status.State != BtsmtlScenePlayState.Faulted)
            {
                SetStatus(
                    BtsmtlScenePlayState.Stopping,
                    BtsmtlScenePlayOperation.Stop,
                    m_Request.Identity,
                    m_Status.SceneGeneration,
                    BtsmtlScenePlayFailureStage.None,
                    string.Empty,
                    string.Empty);
                PersistCoordinatorState(m_Status.SceneGeneration);
            }
        }

        void OnLauncherStateChanged(EditorPlayModeSceneLaunchResult result)
        {
            if (!m_HasRequest || result.RequestId != m_Request.Identity.RequestId)
                return;
            if (result.Code == EditorPlayModeSceneLaunchResultCode.Failed)
            {
                SetStatus(
                    BtsmtlScenePlayState.Faulted,
                    BtsmtlScenePlayOperation.Stop,
                    m_Request.Identity,
                    m_Status.SceneGeneration,
                    BtsmtlScenePlayFailureStage.Stop,
                    "scene_launch_restore_failed",
                    result.Message);
                m_StopFailure = true;
                PersistFailureState(m_Status);
                PersistTerminalStatus(m_Status);
                ClearActiveRequest();
                return;
            }
            if (result.Code != EditorPlayModeSceneLaunchResultCode.Completed)
                return;
            BtsmtlScenePlayStatus completedStatus = m_StopFailure
                ? new BtsmtlScenePlayStatus(
                    BtsmtlScenePlayState.Faulted,
                    BtsmtlScenePlayOperation.Stop,
                    m_Request.Identity,
                    m_Status.SceneGeneration,
                    m_Status.FailureStage,
                    m_Status.FailureCode,
                    m_Status.FailureMessage)
                : BtsmtlScenePlayStatus.Idle;
            if (m_StopFailure)
                PersistTerminalStatus(completedStatus);
            ClearActiveRequest();
            m_Status = completedStatus;
            StatusChanged?.Invoke(m_Status);
        }

        void OnResetSceneLoaded(AsyncOperation operation)
        {
            if (!m_HasRequest || m_Status.State != BtsmtlScenePlayState.Resetting)
                return;
            m_ContextDescriptor = default;
            m_ActorIds.Clear();
            m_SkillOptions.Clear();
            m_BuildStatus = BtsmtlScenePlayBuildStatus.Idle;
            m_BuildAdoptionHost = null;
            m_BuildActorId = string.Empty;
            m_BuildSourceRevision = string.Empty;
            m_BuildRequestedEpoch = 0;
            m_PreparationPolls = 0;
            m_PreparationStartedAt = EditorApplication.timeSinceStartup;
            m_ResumeAfterPreparation = m_ResetWasPaused;
            SetStatus(
                BtsmtlScenePlayState.Preparing,
                BtsmtlScenePlayOperation.Reset,
                m_Request.Identity,
                NextSceneGeneration(),
                BtsmtlScenePlayFailureStage.None,
                string.Empty,
                string.Empty);
            PersistCoordinatorState(m_Status.SceneGeneration);
        }

        bool TryConnectContext()
        {
            m_SkillOptions.Clear();
            if (BtsmtlScenePlayContextRegistry.IsConflicted(
                    m_Request.Identity.ScenePath,
                    m_Request.Identity.ContextId))
            {
                FailAndStop(
                    m_Status.Operation,
                    BtsmtlScenePlayFailureStage.Context,
                    "preview_context_duplicate",
                    $"Scene Play context '{m_Request.Identity.ContextId}' is registered more than once in '{m_Request.Identity.ScenePath}'.");
                return true;
            }
            if (!BtsmtlScenePlayContextRegistry.TryGet(
                    m_Request.Identity.ScenePath,
                    m_Request.Identity.ContextId,
                    out BtsmtlScenePlayContext context))
                return false;
            if (!context.TryDescribe(
                    out BtsmtlScenePlayContextDescriptor descriptor,
                    out BtsmtlScenePlayContextDiagnostic diagnostic))
            {
                FailAndStop(
                    m_Status.Operation,
                    BtsmtlScenePlayFailureStage.Context,
                    "preview_context_invalid_after_load",
                    diagnostic.Message);
                return true;
            }
            m_ContextDescriptor = descriptor;
            if (descriptor.HasContentRuntime && descriptor.RuntimeOwnerStatus.HasFailure)
            {
                FailAndStop(
                    m_Status.Operation,
                    BtsmtlScenePlayFailureStage.Preparation,
                    string.IsNullOrEmpty(descriptor.RuntimeOwnerStatus.FailureCode)
                        ? "runtime_owner_failed"
                        : descriptor.RuntimeOwnerStatus.FailureCode,
                    string.IsNullOrEmpty(descriptor.RuntimeOwnerStatus.FailureMessage)
                        ? "Formal Runtime Owner preparation failed."
                        : descriptor.RuntimeOwnerStatus.FailureMessage);
                return true;
            }
            if (descriptor.HasCharacterRuntime &&
                descriptor.SessionHost.LifecycleState == SimulationSessionLifecycleState.Failed)
            {
                SimulationSessionFailure failure = descriptor.SessionHost.Failure;
                FailAndStop(
                    m_Status.Operation,
                    BtsmtlScenePlayFailureStage.Preparation,
                    failure?.Code ?? "session_preparation_failed",
                    failure?.Message ?? "Formal Session preparation failed.");
                return true;
            }
            if (descriptor.HasContentRuntime && !descriptor.RuntimeOwnerStatus.IsReady)
                return true;
            if (descriptor.HasCharacterRuntime &&
                descriptor.SessionHost.LifecycleState != SimulationSessionLifecycleState.Active)
                return true;
            if (descriptor.HasCharacterRuntime &&
                !TryPrepareScenePlayDiagnostics(descriptor, out string captureError))
            {
                FailAndStop(
                    m_Status.Operation,
                    BtsmtlScenePlayFailureStage.Target,
                    "preview_diagnostics_capture_failed",
                    captureError);
                return true;
            }
            if (descriptor.HasCharacterRuntime &&
                descriptor.SessionHost.LatestCheckpointTick == 0)
            {
                if (!string.IsNullOrEmpty(descriptor.SessionHost.LastCheckpointFailure))
                {
                    FailAndStop(
                        m_Status.Operation,
                        BtsmtlScenePlayFailureStage.Target,
                        "preview_initial_checkpoint_failed",
                        descriptor.SessionHost.LastCheckpointFailure);
                    return true;
                }
                return true;
            }
            BuildSkillOptions(descriptor);
            BtsmtlScenePlayState nextState = m_ResumeAfterPreparation || m_ResetWasPaused
                ? BtsmtlScenePlayState.Paused
                : BtsmtlScenePlayState.Running;
            if (nextState == BtsmtlScenePlayState.Paused)
                EditorApplication.isPaused = true;
            m_ResumeAfterPreparation = false;
            m_ResetWasPaused = false;
            SetStatus(
                nextState,
                m_Status.Operation,
                m_Request.Identity,
                m_Status.SceneGeneration,
                BtsmtlScenePlayFailureStage.None,
                string.Empty,
                string.Empty);
            return true;
        }

        static bool TryPrepareScenePlayDiagnostics(
            BtsmtlScenePlayContextDescriptor descriptor,
            out string error)
        {
            var targets = new List<RuntimeDiagnosticsTarget>(descriptor.Actors.Count);
            for (int i = 0; i < descriptor.Actors.Count; i++)
            {
                if (!TryResolveDiagnosticsTarget(
                        descriptor.Actors[i].Host,
                        out RuntimeDiagnosticsTarget target,
                        out error))
                    return false;
                targets.Add(target);
            }
            var startedCaptures = new List<RuntimeDiagnosticsTarget>(targets.Count);
            for (int i = 0; i < targets.Count; i++)
            {
                bool wasRecording = targets[i].Store.IsCaptureRecording;
                if (!targets[i].Store.BeginCapture(
                        RuntimeTraceChannel.All,
                        RuntimeDiagnosticsCaptureDetail.Boundary,
                        out _))
                {
                    for (int startedIndex = startedCaptures.Count - 1; startedIndex >= 0; startedIndex--)
                        startedCaptures[startedIndex].Store.EndCapture();
                    error = $"Character Actor '{targets[i].DisplayName}' could not start bounded Runtime Diagnostics capture.";
                    return false;
                }
                if (!wasRecording)
                    startedCaptures.Add(targets[i]);
            }
            error = string.Empty;
            return true;
        }

        static bool TryBeginDiagnosticsCapture(
            CharacterPipelineHost host,
            RuntimeDiagnosticsCaptureDetail detail,
            out string error)
        {
            if (!TryResolveDiagnosticsTarget(host, out RuntimeDiagnosticsTarget target, out error))
                return false;
            if (!target.Store.BeginCapture(
                    RuntimeTraceChannel.All,
                    detail,
                    out _))
            {
                error = $"Character Actor '{host.SimulationActorId}' could not start bounded Runtime Diagnostics capture.";
                return false;
            }
            error = string.Empty;
            return true;
        }

        static bool TryResolveDiagnosticsTarget(
            CharacterPipelineHost host,
            out RuntimeDiagnosticsTarget target,
            out string error)
        {
            target = null;
            if (!host)
            {
                error = "Scene Play diagnostics capture requires a live CharacterPipelineHost.";
                return false;
            }
            if (!RuntimeDiagnosticsTargetRegistry.TryGetByHost(
                    host.GetInstanceID(),
                    out target))
            {
                error = $"Character Actor '{host.SimulationActorId}' has no registered Runtime Diagnostics target.";
                return false;
            }
            error = string.Empty;
            return true;
        }

        void BuildSkillOptions(BtsmtlScenePlayContextDescriptor descriptor)
        {
            m_ActorIds.Clear();
            for (int actorIndex = 0; actorIndex < descriptor.Actors.Count; actorIndex++)
            {
                BtsmtlScenePlayActorDescriptor actor = descriptor.Actors[actorIndex];
                m_ActorIds.Add(actor.ActorId.Value);
                IReadOnlyList<CharacterSkillAuthoringDefinition> skills = actor.Definition.SkillDefinitions;
                for (int skillIndex = 0; skillIndex < skills.Count; skillIndex++)
                {
                    CharacterSkillAuthoringDefinition skill = skills[skillIndex];
                    if (skill == null)
                        continue;
                    m_SkillOptions.Add(new BtsmtlScenePlaySkillOption(
                        actor.ActorId.Value,
                        skill.SkillId,
                        skill.EntryGraphAuthoringId,
                        skill.ActionProfile ? skill.ActionProfile.ActionId : string.Empty,
                        skill.SourceInputRequestId));
                }
            }
            m_SkillOptions.Sort((left, right) =>
            {
                int actor = string.CompareOrdinal(left.ActorId, right.ActorId);
                return actor != 0
                    ? actor
                    : string.CompareOrdinal(left.SkillId, right.SkillId);
            });
            m_ActorIds.Sort(StringComparer.Ordinal);
        }

        static bool TryValidateContextBeforePlay(
            BtsmtlScenePlayRequestIdentity identity,
            out string failureCode,
            out string failureMessage)
        {
            failureCode = string.Empty;
            failureMessage = string.Empty;
            Scene scene = SceneManager.GetSceneByPath(identity.ScenePath);
            bool openedHere = !scene.IsValid() || !scene.isLoaded;
            try
            {
                if (openedHere)
                    scene = EditorSceneManager.OpenScene(identity.ScenePath, OpenSceneMode.Additive);
                BtsmtlScenePlayContext[] contexts = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<BtsmtlScenePlayContext>(true))
                    .Where(context => string.Equals(context.ContextId, identity.ContextId, StringComparison.Ordinal))
                    .ToArray();
                if (contexts.Length == 0)
                {
                    failureCode = "preview_context_missing";
                    failureMessage =
                        $"Scene Play context '{identity.ContextId}' is not declared in '{identity.ScenePath}'.";
                    return false;
                }
                if (contexts.Length != 1)
                {
                    failureCode = "preview_context_duplicate";
                    failureMessage =
                        $"Scene Play context '{identity.ContextId}' is declared more than once in '{identity.ScenePath}'.";
                    return false;
                }
                if (!contexts[0].TryDescribe(
                        out BtsmtlScenePlayContextDescriptor descriptor,
                        out BtsmtlScenePlayContextDiagnostic diagnostic))
                {
                    failureCode = "preview_context_invalid";
                    failureMessage = diagnostic.Message;
                    return false;
                }
                if (!TryValidatePublishedCharacterProducts(
                        descriptor,
                        out failureCode,
                        out failureMessage))
                    return false;
                return true;
            }
            catch (Exception exception)
            {
                failureCode = "preview_context_check_failed";
                failureMessage = exception.Message;
                return false;
            }
            finally
            {
                if (openedHere && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        static bool TryValidatePublishedCharacterProducts(
            BtsmtlScenePlayContextDescriptor descriptor,
            out string failureCode,
            out string failureMessage)
        {
            failureCode = string.Empty;
            failureMessage = string.Empty;
            for (int i = 0; i < descriptor.Actors.Count; i++)
            {
                BtsmtlScenePlayActorDescriptor actor = descriptor.Actors[i];
                if (TryGetUnsavedAuthoringDependency(actor.Definition, out string unsavedPath))
                {
                    failureCode = "preview_character_authoring_unsaved";
                    failureMessage = $"Actor '{actor.ActorId}' authoring dependency '{unsavedPath}' has unsaved changes. Save the Character authoring assets before Scene Play.";
                    return false;
                }
                ProgramRevision sourceRevision;
                ProgramId programId;
                try
                {
                    sourceRevision = CharacterSemanticFrontendCompiler.ComputeSourceRevision(actor.Definition);
                    programId = CharacterSemanticFrontendCompiler.ComputeProgramId(actor.Definition);
                }
                catch (Exception exception)
                {
                    failureCode = "preview_character_product_check_failed";
                    failureMessage = $"Actor '{actor.ActorId}' authoring product check failed: {exception.Message}";
                    return false;
                }
                if (!string.Equals(actor.Program.ProgramId, programId.Value, StringComparison.Ordinal) ||
                    !string.Equals(actor.Projection.ProgramId, actor.Program.ProgramId, StringComparison.Ordinal))
                {
                    failureCode = "preview_character_product_mismatch";
                    failureMessage = $"Actor '{actor.ActorId}' Program and Presentation Projection do not belong to the selected Character Definition.";
                    return false;
                }
                if (!string.Equals(actor.Program.SourceRevision, sourceRevision.Value, StringComparison.Ordinal) ||
                    !string.Equals(actor.Projection.SourceRevision, sourceRevision.Value, StringComparison.Ordinal))
                {
                    failureCode = "preview_character_build_required";
                    failureMessage = $"Actor '{actor.ActorId}' authoring SourceRevision '{sourceRevision.Value}' differs from published Program '{actor.Program.SourceRevision}' or Projection '{actor.Projection.SourceRevision}'. Run the explicit Character Build before Scene Play.";
                    return false;
                }
            }
            return true;
        }

        static bool TryGetUnsavedAuthoringDependency(
            CharacterPipelineDefinition definition,
            out string unsavedPath)
        {
            unsavedPath = string.Empty;
            if (!definition)
                return false;
            string definitionPath = AssetDatabase.GetAssetPath(definition);
            if (string.IsNullOrEmpty(definitionPath))
                return false;
            string[] dependencies = AssetDatabase.GetDependencies(definitionPath, true)
                .Append(definitionPath)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            for (int i = 0; i < dependencies.Length; i++)
            {
                string path = dependencies[i].Replace('\\', '/');
                string extension = Path.GetExtension(path);
                if (!string.Equals(extension, ".asset", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(extension, ".inputactions", StringComparison.OrdinalIgnoreCase))
                    continue;
                Type type = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (type == typeof(CharacterSimulationProgramAsset) ||
                    type == typeof(CharacterPresentationProjectionAsset))
                    continue;
                UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(path);
                if (asset && EditorUtility.IsDirty(asset))
                {
                    unsavedPath = path;
                    return true;
                }
            }
            return false;
        }

        BtsmtlScenePlayCommandResult FailAndStop(
            BtsmtlScenePlayOperation operation,
            BtsmtlScenePlayFailureStage stage,
            string code,
            string message)
        {
            SetStatus(
                BtsmtlScenePlayState.Faulted,
                operation,
                m_Request.Identity,
                m_Status.SceneGeneration,
                stage,
                code,
                message);
            m_StopFailure = true;
            PersistFailureState(m_Status);
            EditorApplication.isPaused = false;
            if (EditorApplication.isPlaying)
                EditorApplication.isPlaying = false;
            return new BtsmtlScenePlayCommandResult(
                BtsmtlScenePlayCommandResultCode.Failed,
                operation,
                m_Status,
                message);
        }

        void ClearActiveRequest()
        {
            m_HasRequest = false;
            m_Request = null;
            m_ContextDescriptor = default;
            m_ActorIds.Clear();
            m_SkillOptions.Clear();
            m_ResetWasPaused = false;
            m_ResumeAfterPreparation = false;
            m_BuildWasPaused = false;
            m_StopFailure = false;
            if (m_CharacterBuildTask == null || m_CharacterBuildTask.IsCompleted)
            {
                m_CharacterBuildTask = null;
                m_CharacterBuildHost = null;
            }
            m_BuildAdoptionHost = null;
            m_BuildActorId = string.Empty;
            m_BuildSourceRevision = string.Empty;
            m_BuildRequestedEpoch = 0;
            m_BuildStartedAt = 0d;
            m_BuildPublishedAt = 0d;
            m_BuildElapsedSeconds = -1d;
            m_LastBuildTimingNotificationAt = 0d;
            m_BuildStatus = BtsmtlScenePlayBuildStatus.Idle;
            m_PreparationPolls = 0;
            SessionState.EraseString(SceneGenerationKey);
            SessionState.EraseBool(ResumeAfterPreparationKey);
            SessionState.EraseBool(ResetWasPausedKey);
            SessionState.EraseBool(StopRequestedKey);
            SessionState.EraseBool(StopFailureKey);
            SessionState.EraseInt(FailureOperationKey);
            SessionState.EraseInt(FailureStageKey);
            SessionState.EraseString(FailureCodeKey);
            SessionState.EraseString(FailureMessageKey);
        }

        static void ClearTerminalStatus()
        {
            SessionState.EraseBool(TerminalFailureKey);
            SessionState.EraseString(TerminalRequestIdKey);
            SessionState.EraseString(TerminalScenePathKey);
            SessionState.EraseString(TerminalContextIdKey);
            SessionState.EraseString(TerminalGenerationKey);
            SessionState.EraseInt(TerminalOperationKey);
            SessionState.EraseInt(TerminalStageKey);
            SessionState.EraseString(TerminalCodeKey);
            SessionState.EraseString(TerminalMessageKey);
        }

        void PersistCoordinatorState(ulong sceneGeneration)
        {
            SessionState.SetString(
                SceneGenerationKey,
                sceneGeneration.ToString(CultureInfo.InvariantCulture));
            SessionState.SetBool(
                ResumeAfterPreparationKey,
                m_ResumeAfterPreparation);
            SessionState.SetBool(
                ResetWasPausedKey,
                m_ResetWasPaused);
            SessionState.SetBool(
                StopRequestedKey,
                m_Status.State == BtsmtlScenePlayState.Stopping);
        }

        static void PersistFailureState(BtsmtlScenePlayStatus status)
        {
            SessionState.SetBool(StopFailureKey, true);
            SessionState.SetInt(FailureOperationKey, (int)status.Operation);
            SessionState.SetInt(FailureStageKey, (int)status.FailureStage);
            SessionState.SetString(FailureCodeKey, status.FailureCode);
            SessionState.SetString(FailureMessageKey, status.FailureMessage);
        }

        static void PersistTerminalStatus(BtsmtlScenePlayStatus status)
        {
            SessionState.SetBool(TerminalFailureKey, true);
            SessionState.SetString(
                TerminalRequestIdKey,
                status.Identity.RequestId.ToString("N"));
            SessionState.SetString(TerminalScenePathKey, status.Identity.ScenePath);
            SessionState.SetString(TerminalContextIdKey, status.Identity.ContextId);
            SessionState.SetString(
                TerminalGenerationKey,
                status.SceneGeneration.ToString(CultureInfo.InvariantCulture));
            SessionState.SetInt(TerminalOperationKey, (int)status.Operation);
            SessionState.SetInt(TerminalStageKey, (int)status.FailureStage);
            SessionState.SetString(TerminalCodeKey, status.FailureCode);
            SessionState.SetString(TerminalMessageKey, status.FailureMessage);
        }

        static ulong ReadSceneGeneration()
        {
            return ulong.TryParse(
                       SessionState.GetString(SceneGenerationKey, string.Empty),
                       NumberStyles.None,
                       CultureInfo.InvariantCulture,
                       out ulong value) &&
                   value != 0
                ? value
                : 1;
        }

        BtsmtlScenePlayCommandResult RejectConfiguration(
            BtsmtlScenePlayOperation operation,
            string code,
            string message,
            BtsmtlScenePlayRequestIdentity identity = default,
            BtsmtlScenePlayFailureStage failureStage = BtsmtlScenePlayFailureStage.Context)
        {
            SetStatus(
                BtsmtlScenePlayState.Faulted,
                operation,
                identity != default ? identity : m_HasRequest ? m_Request.Identity : default,
                m_Status.SceneGeneration,
                failureStage,
                code,
                message);
            return new BtsmtlScenePlayCommandResult(
                BtsmtlScenePlayCommandResultCode.RejectedConfiguration,
                operation,
                m_Status,
                message);
        }

        BtsmtlScenePlayCommandResult Rejected(
            BtsmtlScenePlayOperation operation,
            BtsmtlScenePlayCommandResultCode code,
            string message) =>
            new BtsmtlScenePlayCommandResult(code, operation, m_Status, message);

        BtsmtlScenePlayCommandResult Accepted(BtsmtlScenePlayOperation operation) =>
            new BtsmtlScenePlayCommandResult(
                BtsmtlScenePlayCommandResultCode.Accepted,
                operation,
                m_Status,
                string.Empty);

        BtsmtlScenePlaySkillRequestResult RejectSkill(
            BtsmtlScenePlaySkillRequestResultCode code,
            string actorId,
            string skillId,
            string inputRequestId,
            string message) =>
            new BtsmtlScenePlaySkillRequestResult(
                code,
                actorId,
                skillId,
                 inputRequestId,
                 0,
                 m_Status.SceneGeneration,
                 message);

        ulong NextSceneGeneration()
        {
            ulong generation = m_Status.SceneGeneration + 1;
            return generation == 0 ? 1 : generation;
        }

        void SetStatus(
            BtsmtlScenePlayState state,
            BtsmtlScenePlayOperation operation,
            BtsmtlScenePlayRequestIdentity identity,
            ulong sceneGeneration,
            BtsmtlScenePlayFailureStage failureStage,
            string failureCode,
            string failureMessage)
        {
            m_Status = new BtsmtlScenePlayStatus(
                state,
                operation,
                identity,
                sceneGeneration,
                failureStage,
                failureCode,
                failureMessage);
            StatusChanged?.Invoke(m_Status);
        }
    }
}
