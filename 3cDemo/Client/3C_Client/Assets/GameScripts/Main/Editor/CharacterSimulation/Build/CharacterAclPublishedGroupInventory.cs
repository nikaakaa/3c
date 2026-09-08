using System;
using System.Collections.Generic;
using System.IO;
using ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation.ACL;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal enum CharacterAclPublishedGroupFileState : byte
    {
        Complete = 1,
        Partial = 2,
        Orphan = 3
    }

    internal sealed class CharacterAclPublishedGroupInventoryEntry
    {
        readonly List<string> m_Paths = new List<string>();

        internal CharacterAclPublishedGroupInventoryEntry(
            string stem,
            string folder)
        {
            Stem = stem;
            Folder = folder;
        }

        internal string Stem { get; }
        internal string Folder { get; }
        internal IReadOnlyList<string> Paths => m_Paths;
        internal CharacterAclAnimationResource Resource { get; private set; }
        internal CharacterAclPublishedGroupFileState State { get; private set; }

        internal void AddPath(string path)
        {
            if (!m_Paths.Contains(path))
                m_Paths.Add(path);
        }

        internal void Resolve()
        {
            string assetPath = $"{Folder}/{Stem}.asset";
            bool hasAsset = m_Paths.Contains(assetPath);
            if (hasAsset)
            {
                try
                {
                    AssetDatabase.ImportAsset(
                        assetPath,
                        ImportAssetOptions.ForceSynchronousImport);
                    Resource =
                        AssetDatabase.LoadAssetAtPath<CharacterAclAnimationResource>(
                            assetPath);
                }
                catch
                {
                    Resource = null;
                }
            }
            bool complete = false;
            try
            {
                complete = hasAsset && Resource &&
                    Resource.ValidateRuntime().IsReady &&
                    HasExactDeploymentFiles();
            }
            catch
            {
                complete = false;
            }
            State = complete
                ? CharacterAclPublishedGroupFileState.Complete
                : hasAsset
                    ? CharacterAclPublishedGroupFileState.Partial
                    : CharacterAclPublishedGroupFileState.Orphan;
        }

        bool HasExactDeploymentFiles()
        {
            if (Resource.Manifest == null || Resource.GroupClipCount == 0)
                return false;
            var expected = new HashSet<string>(StringComparer.Ordinal)
            {
                $"{Folder}/{Stem}.asset",
                $"{Folder}/{Stem}.quality.json"
            };
            for (int i = 0; i < Resource.GroupClipCount; i++)
            {
                CharacterAclAnimationResourceManifest manifest =
                    Resource.GetGroupManifest(i);
                if (manifest == null)
                    return false;
                expected.Add($"{Folder}/{Stem}.transform.{i}.bytes");
                if (manifest.Scalar.Exists)
                    expected.Add($"{Folder}/{Stem}.scalar.{i}.bytes");
            }
            if (Resource.Manifest.DatabaseHeader.Exists)
                expected.Add($"{Folder}/{Stem}.database.bytes");
            if (Resource.Manifest.BulkMedium.Exists)
                expected.Add($"{Folder}/{Stem}.medium.bytes");
            if (Resource.Manifest.BulkLow.Exists)
                expected.Add($"{Folder}/{Stem}.low.bytes");
            if (expected.Count != m_Paths.Count)
                return false;
            for (int i = 0; i < m_Paths.Count; i++)
            {
                if (!expected.Contains(m_Paths[i]))
                    return false;
            }
            return true;
        }
    }

    internal sealed class CharacterAclPublishedGroupInventory
    {
        readonly Dictionary<string, CharacterAclPublishedGroupInventoryEntry>
            m_ByStem =
                new Dictionary<string, CharacterAclPublishedGroupInventoryEntry>(
                    StringComparer.Ordinal);

        CharacterAclPublishedGroupInventory(
            IReadOnlyList<CharacterAclPublishedGroupInventoryEntry> groups)
        {
            Groups = groups;
            for (int i = 0; i < groups.Count; i++)
                m_ByStem.Add(groups[i].Stem, groups[i]);
        }

        internal IReadOnlyList<CharacterAclPublishedGroupInventoryEntry> Groups {
            get;
        }

        internal bool TryGet(
            string stem,
            out CharacterAclPublishedGroupInventoryEntry entry) =>
            m_ByStem.TryGetValue(stem, out entry);

        internal static CharacterAclPublishedGroupInventory Scan(
            string folder,
            string filePrefix)
        {
            if (string.IsNullOrWhiteSpace(folder))
                throw new ArgumentException(
                    "ACL published group inventory folder is required.",
                    nameof(folder));
            if (string.IsNullOrWhiteSpace(filePrefix))
                throw new ArgumentException(
                    "ACL published group inventory prefix is required.",
                    nameof(filePrefix));
            var groups =
                new Dictionary<string, CharacterAclPublishedGroupInventoryEntry>(
                    StringComparer.Ordinal);
            string fullFolder =
                CharacterAclAnimationArtifactFileStore.ToProjectFilePath(folder);
            if (!Directory.Exists(fullFolder))
                return new CharacterAclPublishedGroupInventory(
                    Array.Empty<CharacterAclPublishedGroupInventoryEntry>());
            string[] files = Directory.GetFiles(
                fullFolder,
                "*",
                SearchOption.TopDirectoryOnly);
            for (int i = 0; i < files.Length; i++)
            {
                string fileName = Path.GetFileName(files[i]);
                string logicalName = fileName.EndsWith(
                        ".meta",
                        StringComparison.OrdinalIgnoreCase)
                    ? fileName.Substring(
                        0,
                        fileName.Length - ".meta".Length)
                    : fileName;
                if (!logicalName.StartsWith(
                        filePrefix,
                        StringComparison.Ordinal))
                    continue;
                int separator = logicalName.IndexOf('.');
                string stem = separator < 0
                    ? logicalName
                    : logicalName.Substring(0, separator);
                if (!groups.TryGetValue(stem, out CharacterAclPublishedGroupInventoryEntry entry))
                {
                    entry = new CharacterAclPublishedGroupInventoryEntry(
                        stem,
                        folder.TrimEnd('/'));
                    groups.Add(stem, entry);
                }
                entry.AddPath($"{folder.TrimEnd('/')}/{logicalName}");
            }
            var timingWatch = System.Diagnostics.Stopwatch.StartNew();
            var ordered = new List<CharacterAclPublishedGroupInventoryEntry>(
                groups.Values);
            ordered.Sort(
                (left, right) =>
                    string.CompareOrdinal(left.Stem, right.Stem));
            for (int i = 0; i < ordered.Count; i++)
                ordered[i].Resolve();
            UnityEngine.Debug.Log(
                $"[计时] ACL清单扫描 {folder} 组数{ordered.Count} {timingWatch.ElapsedMilliseconds}ms");
            return new CharacterAclPublishedGroupInventory(ordered);
        }

    }
}
