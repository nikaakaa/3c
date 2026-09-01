using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThirdPersonCharacter.Editor.ProductBuild;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal readonly struct NetworkTestCandidateCatalogEntry
    {
        public NetworkTestCandidateCatalogEntry(
            string candidateRoot,
            string manifestPath,
            NetworkTestProductBuildManifest manifest)
        {
            CandidateRoot = candidateRoot;
            ManifestPath = manifestPath;
            Manifest = manifest;
        }

        public string CandidateRoot { get; }
        public string ManifestPath { get; }
        public NetworkTestProductBuildManifest Manifest { get; }
    }

    internal static class NetworkTestCandidateCatalog
    {
        public static NetworkTestCandidateCatalogEntry[] Read(INetworkTestProductBuildAdapter adapter)
        {
            string productRoot = NetworkTestProductBuildWorkflow.RequireProductRoot(
                ClientBuildArtifactLayout.NetworkRoot,
                adapter.OutputDirectoryName);
            if (!Directory.Exists(productRoot))
                return Array.Empty<NetworkTestCandidateCatalogEntry>();
            var result = new List<NetworkTestCandidateCatalogEntry>();
            foreach (string candidateRoot in Directory.GetDirectories(productRoot))
            {
                string candidateId = Path.GetFileName(candidateRoot);
                string manifestPath = Path.Combine(candidateRoot, adapter.ManifestFileName);
                if (!File.Exists(manifestPath))
                    throw new InvalidOperationException($"Network Test Candidate directory has no manifest: {candidateRoot}");
                NetworkTestProductBuildManifest manifest = NetworkTestProductBuildWorkflow.ReadCandidate(
                    candidateRoot,
                    manifestPath,
                    adapter.ProductId,
                    candidateId);
                result.Add(new NetworkTestCandidateCatalogEntry(candidateRoot, manifestPath, manifest));
            }
            return result.OrderBy(value => value.Manifest.candidateId, StringComparer.Ordinal).ToArray();
        }

        public static void Remove(INetworkTestProductBuildAdapter adapter, string candidateId)
        {
            string productRoot = NetworkTestProductBuildWorkflow.RequireProductRoot(
                ClientBuildArtifactLayout.NetworkRoot,
                adapter.OutputDirectoryName);
            string candidateRoot = NetworkTestProductBuildWorkflow.RequireCandidateRoot(productRoot, candidateId);
            string manifestPath = Path.Combine(candidateRoot, adapter.ManifestFileName);
            NetworkTestProductBuildManifest manifest = NetworkTestProductBuildWorkflow.ReadCandidate(
                candidateRoot,
                manifestPath,
                adapter.ProductId,
                candidateId);
            string runRoot = Path.Combine(
                ClientBuildArtifactLayout.NetworkRoot,
                "RunLogs",
                adapter.OutputDirectoryName);
            if (Directory.Exists(runRoot))
            {
                foreach (string run in Directory.GetDirectories(runRoot))
                {
                    string runManifestPath = Path.Combine(run, "RunManifest.json");
                    string runStatusPath = Path.Combine(run, "RunStatus.json");
                    if (!File.Exists(runManifestPath) || !File.Exists(runStatusPath))
                        continue;
                    NetworkTestRunManifestDocument runManifest = JsonUtility.FromJson<NetworkTestRunManifestDocument>(
                        File.ReadAllText(runManifestPath));
                    NetworkTestRunStatusDocument status = JsonUtility.FromJson<NetworkTestRunStatusDocument>(
                        File.ReadAllText(runStatusPath));
                    if (runManifest != null && status != null && runManifest.candidateId == manifest.candidateId &&
                        status.state is "Preparing" or "Starting" or "Running" or "Stopping")
                        throw new InvalidOperationException($"Candidate is owned by active Run '{runManifest.runId}'.");
                }
            }
            Directory.Delete(candidateRoot, true);
        }
    }
}
