using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ThirdPersonCharacter.Editor.ProductBuild;
using ThirdPersonCharacter.Editor.ProductStartup;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal enum NetworkTestRuntimeArtifactKind
    {
        UnityPlayer = 1,
        ManagedExecutable = 2
    }

    internal interface INetworkTestProductBuildAdapter
    {
        string ProductId { get; }
        string DisplayName { get; }
        string OutputDirectoryName { get; }
        string PlayerBuildWorkspaceDirectoryName { get; }
        string ManifestFileName { get; }
        void PrepareBuildInputs(NetworkTestProductContext context);
        NetworkTestProductDescriptor CreateDescriptor(NetworkTestProductContext context);
        IReadOnlyList<NetworkTestRuntimeArtifactResult> PublishAdditionalArtifacts(
            NetworkTestProductContext context,
            NetworkTestProductDescriptor descriptor,
            string productRoot,
            string buildId);
        void ValidateProduct(
            NetworkTestProductContext context,
            NetworkTestProductDescriptor descriptor,
            NetworkTestProductBuildManifest manifest);
    }

    internal sealed class NetworkTestProductBuildRequest
    {
        public NetworkTestProductBuildRequest(INetworkTestProductBuildAdapter adapter)
        {
            Adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        }

        public INetworkTestProductBuildAdapter Adapter { get; }
    }

    internal sealed class NetworkTestProductRunRequest
    {
        public NetworkTestProductRunRequest(INetworkTestProductBuildAdapter adapter, bool stopExisting)
        {
            Adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            StopExisting = stopExisting;
        }

        public INetworkTestProductBuildAdapter Adapter { get; }
        public bool StopExisting { get; }
    }

    internal sealed class NetworkTestProductContext
    {
        public NetworkTestProductContext(
            string projectRoot,
            string repositoryRoot,
            string productRoot,
            NetworkTestExternalProcessExecutor processes)
        {
            ProjectRoot = Path.GetFullPath(projectRoot);
            RepositoryRoot = Path.GetFullPath(repositoryRoot);
            ProductRoot = Path.GetFullPath(productRoot);
            Processes = processes ?? throw new ArgumentNullException(nameof(processes));
        }

        public string ProjectRoot { get; }
        public string RepositoryRoot { get; }
        public string ProductRoot { get; }
        public NetworkTestExternalProcessExecutor Processes { get; }
    }

    internal sealed class NetworkTestProductDescriptor
    {
        public NetworkTestProductDescriptor(
            string productId,
            string displayName,
            string outputDirectoryName,
            string manifestFileName,
            string[] playerScenes,
            BuildTarget playerTarget,
            BuildTargetGroup playerTargetGroup,
            BuildOptions playerBuildOptions,
            string playerBuildOptionsIdentity,
            ScriptingImplementation scriptingBackend,
            string launchScriptRelativePath,
            string programIdentity,
            string pipelineIdentity,
            string networkModelIdentity,
            string runtimeTopologyIdentity,
            string playerRoleId,
            string playerProductId,
            NetworkTestProductManifestField[] fields)
        {
            ProductId = Require(productId, nameof(productId));
            DisplayName = Require(displayName, nameof(displayName));
            OutputDirectoryName = RequireSegment(outputDirectoryName, nameof(outputDirectoryName));
            ManifestFileName = RequireSegment(manifestFileName, nameof(manifestFileName));
            PlayerScenes = playerScenes?.ToArray() ?? Array.Empty<string>();
            if (PlayerScenes.Length == 0 || PlayerScenes.Any(string.IsNullOrWhiteSpace) ||
                PlayerScenes.Distinct(StringComparer.Ordinal).Count() != PlayerScenes.Length)
                throw new ArgumentException("Network Test Product requires a unique non-empty Player scene list.", nameof(playerScenes));
            PlayerTarget = playerTarget;
            PlayerTargetGroup = playerTargetGroup;
            PlayerBuildOptions = playerBuildOptions;
            PlayerBuildOptionsIdentity = Require(playerBuildOptionsIdentity, nameof(playerBuildOptionsIdentity));
            ScriptingBackend = scriptingBackend;
            LaunchScriptRelativePath = NormalizeRelative(launchScriptRelativePath, nameof(launchScriptRelativePath));
            ProgramIdentity = Require(programIdentity, nameof(programIdentity));
            PipelineIdentity = Require(pipelineIdentity, nameof(pipelineIdentity));
            NetworkModelIdentity = Require(networkModelIdentity, nameof(networkModelIdentity));
            RuntimeTopologyIdentity = Require(runtimeTopologyIdentity, nameof(runtimeTopologyIdentity));
            PlayerRoleId = Require(playerRoleId, nameof(playerRoleId));
            PlayerProductId = Require(playerProductId, nameof(playerProductId));
            Fields = FreezeFields(fields);
        }

        public string ProductId { get; }
        public string DisplayName { get; }
        public string OutputDirectoryName { get; }
        public string ManifestFileName { get; }
        public string[] PlayerScenes { get; }
        public BuildTarget PlayerTarget { get; }
        public BuildTargetGroup PlayerTargetGroup { get; }
        public BuildOptions PlayerBuildOptions { get; }
        public string PlayerBuildOptionsIdentity { get; }
        public ScriptingImplementation ScriptingBackend { get; }
        public string LaunchScriptRelativePath { get; }
        public string ProgramIdentity { get; }
        public string PipelineIdentity { get; }
        public string NetworkModelIdentity { get; }
        public string RuntimeTopologyIdentity { get; }
        public string PlayerRoleId { get; }
        public string PlayerProductId { get; }
        public NetworkTestProductManifestField[] Fields { get; }

        static NetworkTestProductManifestField[] FreezeFields(NetworkTestProductManifestField[] fields)
        {
            NetworkTestProductManifestField[] values = fields?.ToArray() ?? Array.Empty<NetworkTestProductManifestField>();
            Array.Sort(values, (left, right) => string.CompareOrdinal(left.key, right.key));
            for (int i = 0; i < values.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(values[i]?.key) || string.IsNullOrWhiteSpace(values[i].value))
                    throw new ArgumentException("Network Test Product manifest fields require key and value.", nameof(fields));
                if (i > 0 && string.Equals(values[i - 1].key, values[i].key, StringComparison.Ordinal))
                    throw new ArgumentException($"Network Test Product manifest field '{values[i].key}' is duplicated.", nameof(fields));
            }
            return values;
        }

        static string Require(string value, string parameter) => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Network Test Product identity is required.", parameter)
            : value.Trim();

        static string RequireSegment(string value, string parameter)
        {
            string result = Require(value, parameter);
            if (result.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || result.Contains('/') || result.Contains('\\'))
                throw new ArgumentException("Network Test Product path segment is invalid.", parameter);
            return result;
        }

        static string NormalizeRelative(string value, string parameter)
        {
            string result = Require(value, parameter).Replace('\\', '/');
            if (Path.IsPathRooted(result) || result == ".." || result.StartsWith("../", StringComparison.Ordinal) ||
                result.Contains("/../", StringComparison.Ordinal))
                throw new ArgumentException("Network Test Product relative path is invalid.", parameter);
            return result;
        }
    }

    internal sealed class NetworkTestRuntimeArtifactResult
    {
        public NetworkTestRuntimeArtifactResult(
            string roleId,
            NetworkTestRuntimeArtifactKind kind,
            string productId,
            string rootRelativePath,
            string entryPointRelativePath,
            string configurationIdentity,
            string manifestRelativePath,
            string manifestHash,
            NetworkTestProductManifestField[] fields)
        {
            RoleId = Require(roleId, nameof(roleId));
            if (!Enum.IsDefined(typeof(NetworkTestRuntimeArtifactKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            Kind = kind;
            ProductId = Require(productId, nameof(productId));
            RootRelativePath = NormalizeRelative(rootRelativePath, nameof(rootRelativePath));
            EntryPointRelativePath = NormalizeRelative(entryPointRelativePath, nameof(entryPointRelativePath));
            ConfigurationIdentity = Require(configurationIdentity, nameof(configurationIdentity));
            ManifestRelativePath = manifestRelativePath ?? string.Empty;
            ManifestHash = manifestHash ?? string.Empty;
            Fields = fields ?? Array.Empty<NetworkTestProductManifestField>();
            if (ManifestRelativePath.Length == 0 != (ManifestHash.Length == 0))
                throw new ArgumentException("Runtime artifact manifest path and hash must be supplied together.");
        }

        public string RoleId { get; }
        public NetworkTestRuntimeArtifactKind Kind { get; }
        public string ProductId { get; }
        public string RootRelativePath { get; }
        public string EntryPointRelativePath { get; }
        public string ConfigurationIdentity { get; }
        public string ManifestRelativePath { get; }
        public string ManifestHash { get; }
        public NetworkTestProductManifestField[] Fields { get; }

        static string Require(string value, string parameter) => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Runtime artifact identity is required.", parameter)
            : value.Trim();

        static string NormalizeRelative(string value, string parameter)
        {
            string result = Require(value, parameter).Replace('\\', '/');
            if (Path.IsPathRooted(result) || result == ".." || result.StartsWith("../", StringComparison.Ordinal) ||
                result.Contains("/../", StringComparison.Ordinal))
                throw new ArgumentException("Runtime artifact path is invalid.", parameter);
            return result;
        }
    }

    internal sealed class NetworkTestExternalProcessResult
    {
        public NetworkTestExternalProcessResult(
            string executable,
            string arguments,
            string workingDirectory,
            int exitCode,
            string standardOutput,
            string standardError)
        {
            Executable = executable;
            Arguments = arguments;
            WorkingDirectory = workingDirectory;
            ExitCode = exitCode;
            StandardOutput = standardOutput ?? string.Empty;
            StandardError = standardError ?? string.Empty;
        }

        public string Executable { get; }
        public string Arguments { get; }
        public string WorkingDirectory { get; }
        public int ExitCode { get; }
        public string StandardOutput { get; }
        public string StandardError { get; }

        public string RequireSuccess(string productId)
        {
            if (ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Network Test Product '{productId}' process failed.\n" +
                    $"Command: {Executable} {Arguments}\nWorkingDirectory: {WorkingDirectory}\n" +
                    $"ExitCode: {ExitCode}\nStdout:\n{StandardOutput}\nStderr:\n{StandardError}");
            }
            return string.IsNullOrWhiteSpace(StandardError)
                ? StandardOutput.Trim()
                : $"{StandardOutput}\n{StandardError}".Trim();
        }
    }

    internal sealed class NetworkTestExternalProcessExecutor
    {
        const string DotNetBuildFlags = "--disable-build-servers /nr:false /p:UseSharedCompilation=false";
        const int LauncherTimeoutMilliseconds = 60000;

        public NetworkTestExternalProcessResult Execute(string executable, string arguments, string workingDirectory)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments ?? string.Empty,
                WorkingDirectory = Path.GetFullPath(workingDirectory),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using Process process = Process.Start(startInfo) ??
                throw new InvalidOperationException($"Failed to start process: {executable}");
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            return new NetworkTestExternalProcessResult(
                executable,
                arguments,
                startInfo.WorkingDirectory,
                process.ExitCode,
                outputTask.GetAwaiter().GetResult(),
                errorTask.GetAwaiter().GetResult());
        }

        public NetworkTestExternalProcessResult ExecuteLauncher(
            string executable,
            string arguments,
            string workingDirectory)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments ?? string.Empty,
                WorkingDirectory = Path.GetFullPath(workingDirectory),
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = true
            };
            using Process process = Process.Start(startInfo) ??
                throw new InvalidOperationException($"Failed to start launcher process: {executable}");
            if (!process.WaitForExit(LauncherTimeoutMilliseconds))
            {
                try
                {
                    process.Kill();
                }
                catch (InvalidOperationException)
                {
                }
                throw new TimeoutException(
                    $"Launcher process did not finish within {LauncherTimeoutMilliseconds / 1000} seconds: {executable} {arguments}");
            }
            return new NetworkTestExternalProcessResult(
                executable,
                arguments,
                startInfo.WorkingDirectory,
                process.ExitCode,
                string.Empty,
                string.Empty);
        }

        public string ExecuteDotNetBuild(string productId, string arguments, string workingDirectory)
        {
            NetworkTestExternalProcessResult build = null;
            Exception buildFailure = null;
            try
            {
                build = Execute("dotnet", $"{arguments} {DotNetBuildFlags}", workingDirectory);
                build.RequireSuccess(productId);
            }
            catch (Exception exception)
            {
                buildFailure = exception;
            }

            NetworkTestExternalProcessResult shutdown = Execute("dotnet", "build-server shutdown", workingDirectory);
            if (shutdown.ExitCode != 0)
            {
                var shutdownFailure = new InvalidOperationException(
                    $"Network Test Product '{productId}' failed to shut down dotnet build servers.\n" +
                    $"ExitCode: {shutdown.ExitCode}\nStdout:\n{shutdown.StandardOutput}\nStderr:\n{shutdown.StandardError}");
                if (buildFailure != null)
                    throw new AggregateException(buildFailure, shutdownFailure);
                throw shutdownFailure;
            }
            if (buildFailure != null)
                throw buildFailure;
            return build?.RequireSuccess(productId) ?? string.Empty;
        }

        public static string Quote(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Process argument cannot be empty.", nameof(value));
            return $"\"{value.Replace("\"", "\\\"")}\"";
        }
    }

    internal static class NetworkTestEditorSceneSetup
    {
        public static T Preserve<T>(string operationName, Func<T> operation)
        {
            if (string.IsNullOrWhiteSpace(operationName))
                throw new ArgumentException("Editor scene operation requires an explicit name.", nameof(operationName));
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                throw new OperationCanceledException($"{operationName} was cancelled before saving the current scene setup.");
            SceneSetup[] sceneSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                return operation();
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
            }
        }

        public static void Preserve(string operationName, Action operation)
        {
            Preserve<object>(operationName, () =>
            {
                operation();
                return null;
            });
        }
    }

    internal static class NetworkTestProductBuildWorkflow
    {
        const int ManifestSchemaVersion = 2;

        public static void Build(NetworkTestProductBuildRequest request)
        {
            RequireEditorIdle(request.Adapter.DisplayName, "build");
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string repositoryRoot = Path.GetFullPath(Path.Combine(projectRoot, "..", "..", ".."));
            string networkRoot = ClientBuildArtifactLayout.NetworkRoot;
            ValidateProductCatalog(networkRoot);
            string finalRoot = RequireProductRoot(networkRoot, request.Adapter.OutputDirectoryName);
            string playerBuildWorkspaceRoot = RequirePlayerBuildWorkspaceRoot(
                networkRoot,
                request.Adapter.PlayerBuildWorkspaceDirectoryName);
            string stagingRoot = CreateTransientRoot(networkRoot, "s");
            var processes = new NetworkTestExternalProcessExecutor();
            var stagingContext = new NetworkTestProductContext(projectRoot, repositoryRoot, stagingRoot, processes);
            string buildId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            ScriptingImplementation previousBackend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Standalone);
            try
            {
                NetworkTestProductDescriptor descriptor = NetworkTestEditorSceneSetup.Preserve(
                    $"{request.Adapter.DisplayName} build preparation",
                    () =>
                    {
                        request.Adapter.PrepareBuildInputs(stagingContext);
                        return request.Adapter.CreateDescriptor(stagingContext);
                    });
                RequireAdapterDescriptor(request.Adapter, descriptor);
                RequireCleanDirectory(stagingRoot);
                string playerDirectory = Path.Combine(stagingRoot, "Player");
                string playerExecutable = Path.Combine(playerDirectory, "3C_Client.exe");
                string playerBuildDirectory = Path.Combine(playerBuildWorkspaceRoot, "Player");
                string playerBuildExecutable = Path.Combine(playerBuildDirectory, "3C_Client.exe");
                RequireScenes(projectRoot, descriptor.PlayerScenes);
                Directory.CreateDirectory(playerDirectory);
                Directory.CreateDirectory(playerBuildDirectory);

                EditorUtility.DisplayProgressBar(descriptor.DisplayName, "Building Player", 0.15f);
                if (previousBackend != descriptor.ScriptingBackend)
                    PlayerSettings.SetScriptingBackend(descriptor.PlayerTargetGroup, descriptor.ScriptingBackend);
                BuildReport report;
                using (ProductBuildValidationContext.Enter(ProductBuildKind.NetworkTestPlayer))
                using (CharacterSimulationProgramBuildService.RetainAuthoringDependenciesForPlayerBuild())
                {
                    report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                    {
                        scenes = descriptor.PlayerScenes,
                        locationPathName = playerBuildExecutable,
                        targetGroup = descriptor.PlayerTargetGroup,
                        target = descriptor.PlayerTarget,
                        options = descriptor.PlayerBuildOptions
                    });
                }
                if (report.summary.result != BuildResult.Succeeded || !File.Exists(playerBuildExecutable))
                    throw new InvalidOperationException($"{descriptor.DisplayName} Player build failed: {report.summary.result}");
                MovePlayerProduct(playerBuildDirectory, playerDirectory, Path.GetFileNameWithoutExtension(playerExecutable));
                if (!File.Exists(playerExecutable))
                    throw new InvalidOperationException($"{descriptor.DisplayName} Player publish output is missing.");

                EditorUtility.DisplayProgressBar(descriptor.DisplayName, "Publishing Runtime Artifacts", 0.65f);
                var artifacts = new List<NetworkTestRuntimeArtifactResult>
                {
                    CreatePlayerArtifact(descriptor)
                };
                IReadOnlyList<NetworkTestRuntimeArtifactResult> additional = request.Adapter.PublishAdditionalArtifacts(
                    stagingContext,
                    descriptor,
                    stagingRoot,
                    buildId);
                if (additional != null)
                    artifacts.AddRange(additional);
                NetworkTestRuntimeArtifactResult[] validatedArtifacts = RequireArtifacts(stagingRoot, artifacts);
                NetworkTestProductBuildManifest manifest = CreateManifest(
                    stagingContext,
                    descriptor,
                    validatedArtifacts,
                    buildId);
                string manifestPath = Path.Combine(stagingRoot, descriptor.ManifestFileName);
                File.WriteAllText(manifestPath, JsonUtility.ToJson(manifest, true), new UTF8Encoding(false));
                NetworkTestProductBuildManifest loaded = ValidateCandidate(stagingContext, descriptor, request.Adapter);
                CommitCandidate(stagingRoot, finalRoot, descriptor, request.Adapter, projectRoot, repositoryRoot, processes);
                Debug.Log(
                    $"{descriptor.DisplayName} built. BuildId={loaded.buildId}\n" +
                    $"ProductRoot={finalRoot}\nArtifacts={string.Join(", ", validatedArtifacts.Select(value => value.RoleId))}");
            }
            finally
            {
                if (PlayerSettings.GetScriptingBackend(BuildTargetGroup.Standalone) != previousBackend)
                    PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, previousBackend);
                EditorUtility.ClearProgressBar();
                DeleteDirectoryIfPresent(stagingRoot);
            }
        }

        public static void Run(NetworkTestProductRunRequest request)
        {
            RequireEditorIdle(request.Adapter.DisplayName, "run");
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string repositoryRoot = Path.GetFullPath(Path.Combine(projectRoot, "..", "..", ".."));
            string networkRoot = ClientBuildArtifactLayout.NetworkRoot;
            ValidateProductCatalog(networkRoot);
            string finalRoot = RequireProductRoot(networkRoot, request.Adapter.OutputDirectoryName);
            var processes = new NetworkTestExternalProcessExecutor();
            var context = new NetworkTestProductContext(projectRoot, repositoryRoot, finalRoot, processes);
            NetworkTestProductDescriptor descriptor = request.Adapter.CreateDescriptor(context);
            RequireAdapterDescriptor(request.Adapter, descriptor);
            NetworkTestProductBuildManifest manifest = ValidateCandidate(context, descriptor, request.Adapter);
            string script = Path.Combine(repositoryRoot, descriptor.LaunchScriptRelativePath.Replace('/', Path.DirectorySeparatorChar));
            string arguments =
                $"-NoProfile -ExecutionPolicy Bypass -File {NetworkTestExternalProcessExecutor.Quote(script)} " +
                $"-ProductRoot {NetworkTestExternalProcessExecutor.Quote(finalRoot)}" +
                (request.StopExisting ? " -StopExisting" : string.Empty);
            processes.ExecuteLauncher("powershell.exe", arguments, repositoryRoot).RequireSuccess(descriptor.ProductId);
            Debug.Log(
                $"{descriptor.DisplayName} started. BuildId={manifest.buildId}\nProductRoot={finalRoot}");
        }

        static NetworkTestProductBuildManifest CreateManifest(
            NetworkTestProductContext context,
            NetworkTestProductDescriptor descriptor,
            IReadOnlyList<NetworkTestRuntimeArtifactResult> artifacts,
            string buildId)
        {
            string script = Path.Combine(
                context.RepositoryRoot,
                descriptor.LaunchScriptRelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(script))
                throw new InvalidOperationException($"Network Test Product launch script is missing: {script}");
            return new NetworkTestProductBuildManifest
            {
                schemaVersion = ManifestSchemaVersion,
                buildId = buildId,
                productId = descriptor.ProductId,
                programIdentity = descriptor.ProgramIdentity,
                pipelineIdentity = descriptor.PipelineIdentity,
                networkModelIdentity = descriptor.NetworkModelIdentity,
                runtimeTopologyIdentity = descriptor.RuntimeTopologyIdentity,
                artifacts = artifacts.Select(ToManifest).ToArray(),
                launch = new NetworkTestLaunchManifest
                {
                    scriptPath = descriptor.LaunchScriptRelativePath,
                    scriptHash = NetworkTestArtifactFileUtility.Sha256(script)
                },
                fields = descriptor.Fields,
                files = BuildFileClosure(context.ProductRoot, descriptor.ManifestFileName)
            };
        }

        static NetworkTestProductBuildManifest ValidateCandidate(
            NetworkTestProductContext context,
            NetworkTestProductDescriptor descriptor,
            INetworkTestProductBuildAdapter adapter)
        {
            string manifestPath = Path.Combine(context.ProductRoot, descriptor.ManifestFileName);
            if (!File.Exists(manifestPath))
                throw new InvalidOperationException($"{descriptor.DisplayName} has no completed build manifest. Run Build first.");
            NetworkTestProductBuildManifest manifest = JsonUtility.FromJson<NetworkTestProductBuildManifest>(
                File.ReadAllText(manifestPath, Encoding.UTF8));
            if (manifest == null || manifest.schemaVersion != ManifestSchemaVersion ||
                !string.Equals(manifest.productId, descriptor.ProductId, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(manifest.buildId) ||
                !string.Equals(manifest.programIdentity, descriptor.ProgramIdentity, StringComparison.Ordinal) ||
                !string.Equals(manifest.pipelineIdentity, descriptor.PipelineIdentity, StringComparison.Ordinal) ||
                !string.Equals(manifest.networkModelIdentity, descriptor.NetworkModelIdentity, StringComparison.Ordinal) ||
                !string.Equals(manifest.runtimeTopologyIdentity, descriptor.RuntimeTopologyIdentity, StringComparison.Ordinal))
                throw new InvalidOperationException($"{descriptor.DisplayName} build manifest identity is stale or invalid.");
            RequireManifestArtifacts(context.ProductRoot, manifest.artifacts);
            NetworkTestRuntimeArtifactManifest player = RequireArtifact(manifest, descriptor.PlayerRoleId);
            if (!string.Equals(player.kind, NetworkTestRuntimeArtifactKind.UnityPlayer.ToString(), StringComparison.Ordinal) ||
                !string.Equals(player.productId, descriptor.PlayerProductId, StringComparison.Ordinal) ||
                !string.Equals(player.root, "Player", StringComparison.Ordinal) ||
                !string.Equals(player.entryPoint, "Player/3C_Client.exe", StringComparison.Ordinal))
                throw new InvalidOperationException($"{descriptor.DisplayName} Player artifact does not match its adapter.");
            RequirePlayerArtifactFields(player, descriptor);
            RequireExpectedFields(descriptor.Fields, manifest.fields);
            if (manifest.launch == null ||
                !string.Equals(manifest.launch.scriptPath, descriptor.LaunchScriptRelativePath, StringComparison.Ordinal))
                throw new InvalidOperationException($"{descriptor.DisplayName} launch script identity is invalid.");
            string script = Path.Combine(
                context.RepositoryRoot,
                descriptor.LaunchScriptRelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(script) || !string.Equals(
                    manifest.launch.scriptHash,
                    NetworkTestArtifactFileUtility.Sha256(script),
                    StringComparison.Ordinal))
                throw new InvalidOperationException($"{descriptor.DisplayName} launch script changed after Build.");
            RequireExactClosure(context.ProductRoot, descriptor.ManifestFileName, manifest.files);
            adapter.ValidateProduct(context, descriptor, manifest);
            return manifest;
        }

        static void CommitCandidate(
            string candidateRoot,
            string finalRoot,
            NetworkTestProductDescriptor descriptor,
            INetworkTestProductBuildAdapter adapter,
            string projectRoot,
            string repositoryRoot,
            NetworkTestExternalProcessExecutor processes)
        {
            string networkRoot = Path.GetDirectoryName(finalRoot) ??
                throw new InvalidOperationException("Network Test Product output root has no parent directory.");
            string backupRoot = CreateTransientRoot(networkRoot, "p");
            bool hadPrevious = Directory.Exists(finalRoot);
            if (hadPrevious)
                Directory.Move(finalRoot, backupRoot);
            try
            {
                Directory.Move(candidateRoot, finalRoot);
                var finalContext = new NetworkTestProductContext(projectRoot, repositoryRoot, finalRoot, processes);
                ValidateCandidate(finalContext, descriptor, adapter);
                DeleteDirectoryIfPresent(backupRoot);
            }
            catch
            {
                if (Directory.Exists(finalRoot))
                    Directory.Move(finalRoot, candidateRoot);
                if (hadPrevious && Directory.Exists(backupRoot))
                    Directory.Move(backupRoot, finalRoot);
                throw;
            }
        }

        static NetworkTestProductManifestFile[] BuildFileClosure(string root, string manifestFileName)
        {
            var values = new List<NetworkTestProductManifestFile>();
            foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            {
                string relative = NormalizeRelativePath(Path.GetRelativePath(root, file));
                if (string.Equals(relative, manifestFileName, StringComparison.Ordinal))
                    continue;
                var info = new FileInfo(file);
                values.Add(new NetworkTestProductManifestFile
                {
                    path = relative,
                    length = info.Length,
                    sha256 = NetworkTestArtifactFileUtility.Sha256(file)
                });
            }
            values.Sort((left, right) => string.CompareOrdinal(left.path, right.path));
            return values.ToArray();
        }

        static void RequireExactClosure(
            string root,
            string manifestFileName,
            NetworkTestProductManifestFile[] declared)
        {
            NetworkTestProductManifestFile[] actual = BuildFileClosure(root, manifestFileName);
            NetworkTestProductManifestFile[] expected = declared ?? Array.Empty<NetworkTestProductManifestFile>();
            if (actual.Length != expected.Length)
                throw new InvalidOperationException("Network Test Product exact file closure count does not match its manifest.");
            for (int i = 0; i < actual.Length; i++)
            {
                if (expected[i] == null ||
                    !string.Equals(actual[i].path, expected[i].path, StringComparison.Ordinal) ||
                    actual[i].length != expected[i].length ||
                    !string.Equals(actual[i].sha256, expected[i].sha256, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Network Test Product exact file closure mismatch at '{actual[i].path}'.");
            }
        }

        static NetworkTestRuntimeArtifactResult CreatePlayerArtifact(NetworkTestProductDescriptor descriptor)
        {
            NetworkTestProductManifestField[] fields =
            {
                new NetworkTestProductManifestField { key = "buildOptions", value = descriptor.PlayerBuildOptionsIdentity },
                new NetworkTestProductManifestField { key = "scenes", value = string.Join("|", descriptor.PlayerScenes) },
                new NetworkTestProductManifestField { key = "scriptingBackend", value = descriptor.ScriptingBackend.ToString() },
                new NetworkTestProductManifestField { key = "target", value = descriptor.PlayerTarget.ToString() }
            };
            string configuration = ThirdPersonSimulation.StableHash.Compute(
                "network-test-unity-player/1",
                descriptor.PlayerTarget.ToString(),
                descriptor.PlayerBuildOptionsIdentity,
                descriptor.ScriptingBackend.ToString(),
                string.Join("|", descriptor.PlayerScenes)).Value;
            return new NetworkTestRuntimeArtifactResult(
                descriptor.PlayerRoleId,
                NetworkTestRuntimeArtifactKind.UnityPlayer,
                descriptor.PlayerProductId,
                "Player",
                "3C_Client.exe",
                configuration,
                string.Empty,
                string.Empty,
                fields);
        }

        static NetworkTestRuntimeArtifactResult[] RequireArtifacts(
            string productRoot,
            IEnumerable<NetworkTestRuntimeArtifactResult> source)
        {
            NetworkTestRuntimeArtifactResult[] artifacts = source?.ToArray() ?? Array.Empty<NetworkTestRuntimeArtifactResult>();
            if (artifacts.Length == 0)
                throw new InvalidOperationException("Network Test Product requires at least one runtime artifact.");
            Array.Sort(artifacts, (left, right) => string.CompareOrdinal(left?.RoleId, right?.RoleId));
            var roles = new HashSet<string>(StringComparer.Ordinal);
            var products = new HashSet<string>(StringComparer.Ordinal);
            var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < artifacts.Length; i++)
            {
                NetworkTestRuntimeArtifactResult artifact = artifacts[i] ??
                    throw new InvalidOperationException("Network Test Product contains a missing runtime artifact.");
                if (!roles.Add(artifact.RoleId) || !products.Add(artifact.ProductId) || !roots.Add(artifact.RootRelativePath))
                    throw new InvalidOperationException("Network Test Product runtime artifact identity, ProductId, or root is duplicated.");
                string root = RequireContainedPath(productRoot, artifact.RootRelativePath);
                if (!Directory.Exists(root))
                    throw new InvalidOperationException($"Runtime artifact root is missing: {artifact.RootRelativePath}");
                string entryPoint = RequireContainedPath(root, artifact.EntryPointRelativePath);
                if (!File.Exists(entryPoint))
                    throw new InvalidOperationException($"Runtime artifact entry point is missing: {artifact.EntryPointRelativePath}");
                RequireExpectedFields(artifact.Fields, artifact.Fields);
                if (artifact.ManifestRelativePath.Length == 0)
                    continue;
                string manifest = RequireContainedPath(productRoot, artifact.ManifestRelativePath);
                if (!manifest.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                    !File.Exists(manifest) || !string.Equals(
                        NetworkTestArtifactFileUtility.Sha256(manifest),
                        artifact.ManifestHash,
                        StringComparison.Ordinal))
                    throw new InvalidOperationException($"Runtime artifact manifest is outside its root or has a stale hash: {artifact.RoleId}");
            }
            return artifacts;
        }

        static NetworkTestRuntimeArtifactManifest ToManifest(NetworkTestRuntimeArtifactResult artifact) =>
            new NetworkTestRuntimeArtifactManifest
            {
                roleId = artifact.RoleId,
                kind = artifact.Kind.ToString(),
                productId = artifact.ProductId,
                root = artifact.RootRelativePath,
                entryPoint = NormalizeRelativePath(Path.Combine(artifact.RootRelativePath, artifact.EntryPointRelativePath)),
                configurationIdentity = artifact.ConfigurationIdentity,
                manifestPath = artifact.ManifestRelativePath,
                manifestHash = artifact.ManifestHash,
                fields = artifact.Fields.OrderBy(value => value.key, StringComparer.Ordinal).ToArray()
            };

        static void RequireManifestArtifacts(
            string productRoot,
            NetworkTestRuntimeArtifactManifest[] source)
        {
            NetworkTestRuntimeArtifactManifest[] artifacts = source ?? Array.Empty<NetworkTestRuntimeArtifactManifest>();
            if (artifacts.Length == 0)
                throw new InvalidOperationException("Network Test Product manifest has no runtime artifacts.");
            var roles = new HashSet<string>(StringComparer.Ordinal);
            var products = new HashSet<string>(StringComparer.Ordinal);
            var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string previous = null;
            for (int i = 0; i < artifacts.Length; i++)
            {
                NetworkTestRuntimeArtifactManifest artifact = artifacts[i] ??
                    throw new InvalidOperationException("Network Test Product manifest contains a missing runtime artifact.");
                if (!Enum.TryParse(artifact.kind, false, out NetworkTestRuntimeArtifactKind kind) ||
                    !Enum.IsDefined(typeof(NetworkTestRuntimeArtifactKind), kind) ||
                    string.IsNullOrWhiteSpace(artifact.roleId) || string.IsNullOrWhiteSpace(artifact.productId) ||
                    string.IsNullOrWhiteSpace(artifact.configurationIdentity) ||
                    previous != null && string.CompareOrdinal(previous, artifact.roleId) >= 0 ||
                    !roles.Add(artifact.roleId) || !products.Add(artifact.productId) || !roots.Add(artifact.root))
                    throw new InvalidOperationException("Network Test Product manifest runtime artifact identity is invalid.");
                string root = RequireContainedPath(productRoot, artifact.root);
                string entryPoint = RequireContainedPath(productRoot, artifact.entryPoint);
                if (!Directory.Exists(root) || !entryPoint.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                    !File.Exists(entryPoint))
                    throw new InvalidOperationException($"Network Test Product runtime artifact closure is invalid: {artifact.roleId}");
                RequireExpectedFields(artifact.fields, artifact.fields);
                if (artifact.manifestPath.Length == 0 != (artifact.manifestHash.Length == 0))
                    throw new InvalidOperationException($"Runtime artifact manifest identity is incomplete: {artifact.roleId}");
                if (artifact.manifestPath.Length != 0)
                {
                    string manifest = RequireContainedPath(productRoot, artifact.manifestPath);
                    if (!manifest.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                        !File.Exists(manifest) || !string.Equals(
                            NetworkTestArtifactFileUtility.Sha256(manifest),
                            artifact.manifestHash,
                            StringComparison.Ordinal))
                        throw new InvalidOperationException($"Runtime artifact manifest hash is invalid: {artifact.roleId}");
                }
                previous = artifact.roleId;
            }
        }

        internal static NetworkTestRuntimeArtifactManifest RequireArtifact(
            NetworkTestProductBuildManifest manifest,
            string roleId)
        {
            NetworkTestRuntimeArtifactManifest[] matches = (manifest.artifacts ?? Array.Empty<NetworkTestRuntimeArtifactManifest>())
                .Where(value => value != null && string.Equals(value.roleId, roleId, StringComparison.Ordinal))
                .ToArray();
            return matches.Length == 1
                ? matches[0]
                : throw new InvalidOperationException($"Network Test Product requires exactly one runtime artifact '{roleId}'.");
        }

        static void RequirePlayerArtifactFields(
            NetworkTestRuntimeArtifactManifest player,
            NetworkTestProductDescriptor descriptor)
        {
            RequireExpectedFields(new[]
            {
                new NetworkTestProductManifestField { key = "buildOptions", value = descriptor.PlayerBuildOptionsIdentity },
                new NetworkTestProductManifestField { key = "scenes", value = string.Join("|", descriptor.PlayerScenes) },
                new NetworkTestProductManifestField { key = "scriptingBackend", value = descriptor.ScriptingBackend.ToString() },
                new NetworkTestProductManifestField { key = "target", value = descriptor.PlayerTarget.ToString() }
            }, player.fields);
        }

        static string RequireContainedPath(string root, string relative)
        {
            string normalized = NormalizeRelativePath(relative);
            string fullRoot = Path.GetFullPath(root);
            string full = Path.GetFullPath(Path.Combine(fullRoot, normalized.Replace('/', Path.DirectorySeparatorChar)));
            if (!full.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Network Test Product path escaped its root: {relative}");
            return full;
        }

        static void RequireAdapterDescriptor(
            INetworkTestProductBuildAdapter adapter,
            NetworkTestProductDescriptor descriptor)
        {
            if (descriptor == null ||
                !string.Equals(adapter.ProductId, descriptor.ProductId, StringComparison.Ordinal) ||
                !string.Equals(adapter.DisplayName, descriptor.DisplayName, StringComparison.Ordinal) ||
                !string.Equals(adapter.OutputDirectoryName, descriptor.OutputDirectoryName, StringComparison.Ordinal) ||
                !string.Equals(adapter.ManifestFileName, descriptor.ManifestFileName, StringComparison.Ordinal))
                throw new InvalidOperationException("Network Test Product adapter descriptor identity is inconsistent.");
        }

        static void ValidateProductCatalog(string networkRoot)
        {
            var productIds = new HashSet<string>(StringComparer.Ordinal);
            var outputRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var workspaceRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var manifests = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < NetworkTestProductAdapters.All.Length; i++)
            {
                INetworkTestProductBuildAdapter adapter = NetworkTestProductAdapters.All[i] ??
                    throw new InvalidOperationException("Network Test Product catalog contains an empty adapter.");
                string root = RequireProductRoot(networkRoot, adapter.OutputDirectoryName);
                string workspaceRoot = RequirePlayerBuildWorkspaceRoot(
                    networkRoot,
                    adapter.PlayerBuildWorkspaceDirectoryName);
                string manifest = Path.GetFullPath(Path.Combine(root, adapter.ManifestFileName));
                if (!string.Equals(Path.GetDirectoryName(manifest), root, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Network Test Product manifest escaped its formal Product output root.");
                if (!productIds.Add(adapter.ProductId) || !outputRoots.Add(root) ||
                    !workspaceRoots.Add(workspaceRoot) || !manifests.Add(manifest))
                    throw new InvalidOperationException("Network Test Product catalog contains shared identity or output paths.");
            }
        }

        static string RequireProductRoot(string networkRoot, string directoryName)
        {
            string root = Path.GetFullPath(Path.Combine(networkRoot, directoryName));
            string parent = Path.GetDirectoryName(root) ?? string.Empty;
            if (!string.Equals(parent, Path.GetFullPath(networkRoot), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Network Test Product output root escaped the formal Network build directory.");
            return root;
        }

        static string RequirePlayerBuildWorkspaceRoot(string networkRoot, string directoryName)
        {
            if (string.IsNullOrWhiteSpace(directoryName) || !directoryName.StartsWith(".w-", StringComparison.Ordinal))
                throw new InvalidOperationException("Network Test Product Player build workspace identity is invalid.");
            string root = Path.GetFullPath(Path.Combine(networkRoot, directoryName));
            string parent = Path.GetDirectoryName(root) ?? string.Empty;
            if (!string.Equals(parent, Path.GetFullPath(networkRoot), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Network Test Product Player build workspace escaped the formal Network build directory.");
            return root;
        }

        static string CreateTransientRoot(string networkRoot, string marker)
        {
            string identity = Guid.NewGuid().ToString("N").Substring(0, 12);
            string root = Path.GetFullPath(Path.Combine(networkRoot, $".{marker}-{identity}"));
            if (!string.Equals(Path.GetDirectoryName(root), Path.GetFullPath(networkRoot), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Network Test Product transient root escaped the formal Network build directory.");
            if (Directory.Exists(root) || File.Exists(root))
                throw new InvalidOperationException("Network Test Product transient root identity collided with an existing path.");
            return root;
        }

        static void RequireScenes(string projectRoot, IReadOnlyList<string> scenes)
        {
            for (int i = 0; i < scenes.Count; i++)
            {
                string path = Path.Combine(projectRoot, scenes[i]);
                if (!File.Exists(path))
                    throw new InvalidOperationException($"Network Test Product Player scene is missing: {scenes[i]}");
            }
        }

        static void RequireExpectedFields(
            NetworkTestProductManifestField[] expected,
            NetworkTestProductManifestField[] actual)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (NetworkTestProductManifestField field in actual ?? Array.Empty<NetworkTestProductManifestField>())
            {
                if (field == null || string.IsNullOrWhiteSpace(field.key) || string.IsNullOrWhiteSpace(field.value) ||
                    !values.TryAdd(field.key, field.value))
                    throw new InvalidOperationException("Network Test Product manifest contains invalid or duplicate fields.");
            }
            foreach (NetworkTestProductManifestField field in expected ?? Array.Empty<NetworkTestProductManifestField>())
            {
                if (!values.TryGetValue(field.key, out string value) || !string.Equals(value, field.value, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Network Test Product manifest field '{field.key}' is stale.");
            }
        }

        static string NormalizeRelativePath(string path)
        {
            string value = path.Replace('\\', '/');
            if (Path.IsPathRooted(value) || value == ".." || value.StartsWith("../", StringComparison.Ordinal) ||
                value.Contains("/../", StringComparison.Ordinal))
                throw new InvalidOperationException($"Network Test Product file path is invalid: {path}");
            return value;
        }

        static void RequireCleanDirectory(string path)
        {
            if (Directory.Exists(path))
                throw new InvalidOperationException($"Network Test Product staging directory already exists: {path}");
            Directory.CreateDirectory(path);
        }

        static void MovePlayerProduct(string buildDirectory, string productDirectory, string playerName)
        {
            string backupName = $"{playerName}_BackUpThisFolder_ButDontShipItWithYourGame";
            string burstDebugName = $"{playerName}_BurstDebugInformation_DoNotShip";
            foreach (string source in Directory.GetFileSystemEntries(buildDirectory))
            {
                string name = Path.GetFileName(source);
                if (string.Equals(name, backupName, StringComparison.Ordinal) ||
                    string.Equals(name, burstDebugName, StringComparison.Ordinal))
                    continue;
                string target = Path.Combine(productDirectory, name);
                if (Directory.Exists(source))
                    Directory.Move(source, target);
                else
                    File.Move(source, target);
            }
        }

        static void DeleteDirectoryIfPresent(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }

        static void RequireEditorIdle(string displayName, string operation)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException($"{displayName} cannot {operation} while the Editor is playing or compiling.");
        }
    }

    [Serializable]
    internal sealed class NetworkTestProductBuildManifest
    {
        public int schemaVersion;
        public string buildId = string.Empty;
        public string productId = string.Empty;
        public string programIdentity = string.Empty;
        public string pipelineIdentity = string.Empty;
        public string networkModelIdentity = string.Empty;
        public string runtimeTopologyIdentity = string.Empty;
        public NetworkTestRuntimeArtifactManifest[] artifacts = Array.Empty<NetworkTestRuntimeArtifactManifest>();
        public NetworkTestLaunchManifest launch;
        public NetworkTestProductManifestField[] fields = Array.Empty<NetworkTestProductManifestField>();
        public NetworkTestProductManifestFile[] files = Array.Empty<NetworkTestProductManifestFile>();
    }

    [Serializable]
    internal sealed class NetworkTestRuntimeArtifactManifest
    {
        public string roleId = string.Empty;
        public string kind = string.Empty;
        public string productId = string.Empty;
        public string root = string.Empty;
        public string entryPoint = string.Empty;
        public string configurationIdentity = string.Empty;
        public string manifestPath = string.Empty;
        public string manifestHash = string.Empty;
        public NetworkTestProductManifestField[] fields = Array.Empty<NetworkTestProductManifestField>();
    }

    [Serializable]
    internal sealed class NetworkTestLaunchManifest
    {
        public string scriptPath = string.Empty;
        public string scriptHash = string.Empty;
    }

    [Serializable]
    internal sealed class NetworkTestProductManifestField
    {
        public string key = string.Empty;
        public string value = string.Empty;
    }

    [Serializable]
    internal sealed class NetworkTestProductManifestFile
    {
        public string path = string.Empty;
        public long length;
        public string sha256 = string.Empty;
    }
}
