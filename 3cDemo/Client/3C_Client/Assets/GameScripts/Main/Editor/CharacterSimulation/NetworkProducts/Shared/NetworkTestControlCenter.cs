using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ThirdPersonCharacter.Editor.ProductBuild;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal sealed class NetworkTestControlCenterProductSnapshot
    {
        public INetworkTestProductBuildAdapter Adapter;
        public NetworkTestCandidateCatalogEntry[] Candidates = Array.Empty<NetworkTestCandidateCatalogEntry>();
        public NetworkTestControlCenterRunSnapshot[] Runs = Array.Empty<NetworkTestControlCenterRunSnapshot>();
        public string Error = string.Empty;
    }

    internal sealed class NetworkTestControlCenterRunSnapshot
    {
        public string RunRoot = string.Empty;
        public NetworkTestRunManifestDocument Manifest = new NetworkTestRunManifestDocument();
        public NetworkTestRunStatusDocument Status = new NetworkTestRunStatusDocument();

        public bool IsActive => Status.state is "Preparing" or "Starting" or "Running" or "Stopping";
    }

    internal sealed class NetworkTestControlCenterSnapshot
    {
        public NetworkTestControlCenterProductSnapshot[] Products =
            Array.Empty<NetworkTestControlCenterProductSnapshot>();
    }

    internal sealed class NetworkTestControlCenter
    {
        Task<NetworkTestControlCenterSnapshot> m_Work;
        NetworkTestControlCenterSnapshot m_Snapshot = new NetworkTestControlCenterSnapshot();
        string m_Status = "候选目录尚未刷新。";

        public NetworkTestControlCenterSnapshot Snapshot => m_Snapshot;
        public string Status => m_Status;
        public bool IsWorking => m_Work != null;

        public void Refresh()
        {
            if (m_Work != null)
                return;
            string networkRoot = ClientBuildArtifactLayout.NetworkRoot;
            INetworkTestProductBuildAdapter[] adapters = NetworkTestProductAdapters.All.ToArray();
            m_Status = "正在后台严格校验Candidate与读取Run状态…";
            m_Work = Task.Run(() => ReadSnapshot(networkRoot, adapters));
        }

        public void Remove(INetworkTestProductBuildAdapter adapter, string candidateId)
        {
            if (m_Work != null)
                return;
            string networkRoot = ClientBuildArtifactLayout.NetworkRoot;
            INetworkTestProductBuildAdapter[] adapters = NetworkTestProductAdapters.All.ToArray();
            m_Status = $"正在删除Candidate {candidateId}…";
            m_Work = Task.Run(() =>
            {
                NetworkTestCandidateCatalog.Remove(adapter, candidateId, networkRoot);
                return ReadSnapshot(networkRoot, adapters);
            });
        }

        public bool Poll()
        {
            if (m_Work == null || !m_Work.IsCompleted)
                return false;
            try
            {
                m_Snapshot = m_Work.GetAwaiter().GetResult();
                m_Status = "Candidate与Run状态已刷新。";
            }
            catch (Exception exception)
            {
                m_Status = exception.Message;
            }
            finally
            {
                m_Work = null;
            }
            return true;
        }

        public void Build(INetworkTestProductBuildAdapter adapter, string candidateLabel) =>
            NetworkTestProductBuildWorkflow.Build(new NetworkTestProductBuildRequest(adapter, candidateLabel));

        public void Start(
            INetworkTestProductBuildAdapter adapter,
            string candidateId,
            string slotId) =>
            NetworkTestProductBuildWorkflow.Run(new NetworkTestProductRunRequest(adapter, candidateId, slotId));

        public static void Stop(NetworkTestControlCenterRunSnapshot run)
        {
            string executable = Path.Combine(
                run.Manifest.candidateRoot,
                "Tools",
                "Orchestrator",
                "ThirdPerson.NetworkTest.Orchestrator.exe");
            if (!File.Exists(executable))
                throw new InvalidOperationException("Run所属Candidate的Orchestrator不存在。");
            new NetworkTestExternalProcessExecutor().StartDetached(
                executable,
                $"stop --run {NetworkTestExternalProcessExecutor.Quote(run.RunRoot)}",
                run.Manifest.candidateRoot);
        }

        public static void OpenLogs(NetworkTestControlCenterRunSnapshot run)
        {
            string logs = Path.Combine(run.RunRoot, "Logs");
            if (!Directory.Exists(logs))
                throw new DirectoryNotFoundException($"Run日志目录不存在：{logs}");
            EditorUtility.RevealInFinder(logs);
        }

        public static void OpenGm(NetworkTestControlCenterRunSnapshot run)
        {
            NetworkTestRunProcessDocument gm = (run.Status.processes ?? Array.Empty<NetworkTestRunProcessDocument>())
                .SingleOrDefault(value => value != null && value.roleId == "gm") ??
                throw new InvalidOperationException("此Run没有可用GM进程。");
            using Process process = Process.GetProcessById(gm.processId);
            if (process.HasExited || process.StartTime.ToUniversalTime().Ticks != gm.processStartTimeUtcTicks ||
                process.MainWindowHandle == IntPtr.Zero || !SetForegroundWindow(process.MainWindowHandle))
                throw new InvalidOperationException("GM进程已结束或窗口不可用。");
        }

        static NetworkTestControlCenterSnapshot ReadSnapshot(
            string networkRoot,
            IReadOnlyList<INetworkTestProductBuildAdapter> adapters)
        {
            var products = new NetworkTestControlCenterProductSnapshot[adapters.Count];
            for (int i = 0; i < adapters.Count; i++)
            {
                INetworkTestProductBuildAdapter adapter = adapters[i];
                var product = new NetworkTestControlCenterProductSnapshot
                {
                    Adapter = adapter
                };
                try
                {
                    product.Candidates = NetworkTestCandidateCatalog.Read(adapter, networkRoot);
                    product.Runs = ReadRuns(networkRoot, adapter);
                }
                catch (Exception exception)
                {
                    product.Error = exception.Message;
                }
                products[i] = product;
            }
            return new NetworkTestControlCenterSnapshot
            {
                Products = products
            };
        }

        static NetworkTestControlCenterRunSnapshot[] ReadRuns(
            string networkRoot,
            INetworkTestProductBuildAdapter adapter)
        {
            string root = Path.Combine(networkRoot, "RunLogs", adapter.OutputDirectoryName);
            if (!Directory.Exists(root))
                return Array.Empty<NetworkTestControlCenterRunSnapshot>();
            var runs = new List<NetworkTestControlCenterRunSnapshot>();
            foreach (string runRoot in Directory.GetDirectories(root))
            {
                string manifestPath = Path.Combine(runRoot, "RunManifest.json");
                string statusPath = Path.Combine(runRoot, "RunStatus.json");
                if (!File.Exists(manifestPath) || !File.Exists(statusPath))
                    continue;
                NetworkTestRunManifestDocument manifest = JsonUtility.FromJson<NetworkTestRunManifestDocument>(
                    File.ReadAllText(manifestPath));
                NetworkTestRunStatusDocument status = JsonUtility.FromJson<NetworkTestRunStatusDocument>(
                    File.ReadAllText(statusPath));
                if (manifest == null || status == null || manifest.schemaVersion != 1 || status.schemaVersion != 1 ||
                    manifest.productId != adapter.ProductId || manifest.runId != status.runId ||
                    Path.GetFullPath(manifest.runRoot) != Path.GetFullPath(runRoot))
                    throw new InvalidOperationException($"Network Test Run状态无效：{runRoot}");
                runs.Add(new NetworkTestControlCenterRunSnapshot
                {
                    RunRoot = runRoot,
                    Manifest = manifest,
                    Status = status
                });
            }
            return runs.OrderByDescending(value => value.Manifest.runId, StringComparer.Ordinal).ToArray();
        }

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr window);
    }
}
