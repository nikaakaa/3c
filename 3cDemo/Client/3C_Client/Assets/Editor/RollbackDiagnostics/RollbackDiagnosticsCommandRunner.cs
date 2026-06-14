using System;
using System.IO;
using System.Linq;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ThirdPersonEditor.RollbackDiagnostics
{
    [InitializeOnLoad]
    public static class RollbackDiagnosticsCommandRunner
    {
        const string DefaultScene = "Assets/Scenes/Sandbox.unity";

        static readonly string CommandPath = Path.Combine(ProjectRoot, "Library", "RollbackDiagnostics", "Command.json");
        static readonly string ResultPath = Path.Combine(ProjectRoot, "Library", "RollbackDiagnostics", "Result.json");
        static Command activeCommand;
        static DateTime deadlineUtc;
        static int stage;

        static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;

        static RollbackDiagnosticsCommandRunner()
        {
            EditorApplication.update += Update;
        }

        static void Update()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            if (activeCommand == null)
            {
                TryLoadCommand();
                return;
            }

            if (DateTime.UtcNow > deadlineUtc)
            {
                Finish(false, "timeout");
                return;
            }

            TickActiveCommand();
        }

        static void TryLoadCommand()
        {
            if (!File.Exists(CommandPath))
                return;

            Command command;
            try
            {
                command = JsonUtility.FromJson<Command>(File.ReadAllText(CommandPath));
                File.Delete(CommandPath);
            }
            catch (Exception exception)
            {
                WriteResult(null, false, "invalid-command " + exception.Message);
                return;
            }

            if (command == null || string.IsNullOrWhiteSpace(command.command))
            {
                WriteResult(command, false, "missing-command");
                return;
            }

            activeCommand = command;
            deadlineUtc = DateTime.UtcNow.AddSeconds(Mathf.Max(1, command.timeoutSeconds));
            stage = 0;
            Debug.LogWarning($"ROLLBACK_EDITOR_COMMAND result=START command={command.command} id={command.id}");
        }

        static void TickActiveCommand()
        {
            if (stage == 0)
            {
                string scene = string.IsNullOrWhiteSpace(activeCommand.scene) ? DefaultScene : activeCommand.scene;
                if (!EditorApplication.isPlaying && EditorSceneManager.GetActiveScene().path != scene)
                    EditorSceneManager.OpenScene(scene);

                if (!EditorApplication.isPlaying)
                {
                    stage = 1;
                    EditorApplication.EnterPlaymode();
                    return;
                }

                stage = 2;
                return;
            }

            if (stage == 1)
            {
                if (!EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                    return;

                stage = 2;
                return;
            }

            RunCommand();
        }

        static void RunCommand()
        {
            switch (activeCommand.command)
            {
                case "soak":
                    RunSoak();
                    break;
                case "synctest":
                    RunSynctest();
                    break;
                default:
                    Finish(false, "unknown-command");
                    break;
            }
        }

        static void RunSoak()
        {
            LocalRollbackSoakDebugRunner runner = Object.FindObjectsOfType<LocalRollbackSoakDebugRunner>(true)
                .FirstOrDefault(candidate => candidate != null && candidate.enabled);
            if (runner == null)
            {
                Finish(false, "missing-soak-runner");
                return;
            }

            if (activeCommand.seed > 0)
                runner.Seed = activeCommand.seed;
            if (activeCommand.tickCount > 0)
                runner.TickCount = activeCommand.tickCount;
            if (activeCommand.rollbackFrames > 0)
                runner.RollbackFrames = activeCommand.rollbackFrames;
            runner.StopOnFailure = activeCommand.stopOnFailure;
            runner.RunOnKeyDown = false;
            runner.ApplyReplayResultToScene = false;

            bool success = runner.RunSoak();
            string reason = success ? string.Empty : runner.LastResult.FailureReason;
            Finish(success, reason);
        }

        static void RunSynctest()
        {
            LocalRollbackSynctestDebugRunner runner = Object.FindObjectsOfType<LocalRollbackSynctestDebugRunner>(true)
                .FirstOrDefault(candidate => candidate != null && candidate.enabled);
            if (runner == null)
            {
                Finish(false, "missing-synctest-runner");
                return;
            }

            if (activeCommand.rollbackFrames > 0)
                runner.RollbackFrames = activeCommand.rollbackFrames;
            runner.RunOnKeyDown = false;
            runner.ApplyReplayResultToScene = false;

            bool success = runner.RunDebugSynctest();
            string reason = success ? string.Empty : runner.LastResult.FailureReason;
            Finish(success, reason);
        }

        static void Finish(bool success, string reason)
        {
            Command command = activeCommand;
            WriteResult(command, success, reason);
            Debug.LogWarning($"ROLLBACK_EDITOR_COMMAND result={(success ? "PASS" : "FAIL")} command={command.command} id={command.id} reason={reason}");
            bool exitPlayMode = command.exitPlayMode;
            activeCommand = null;
            stage = 0;
            if (exitPlayMode && EditorApplication.isPlaying)
                EditorApplication.ExitPlaymode();
        }

        static void WriteResult(Command command, bool success, string reason)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ResultPath));
            Result result = new Result
            {
                id = command != null ? command.id : string.Empty,
                command = command != null ? command.command : string.Empty,
                success = success,
                reason = reason ?? string.Empty,
                utc = DateTime.UtcNow.ToString("O")
            };
            File.WriteAllText(ResultPath, JsonUtility.ToJson(result));
        }

        [Serializable]
        sealed class Command
        {
            public string id;
            public string command;
            public string scene = DefaultScene;
            public int timeoutSeconds = 120;
            public int seed = 12345;
            public int tickCount = 600;
            public int rollbackFrames = 8;
            public bool stopOnFailure = true;
            public bool exitPlayMode = true;
        }

        [Serializable]
        sealed class Result
        {
            public string id;
            public string command;
            public bool success;
            public string reason;
            public string utc;
        }
    }
}
