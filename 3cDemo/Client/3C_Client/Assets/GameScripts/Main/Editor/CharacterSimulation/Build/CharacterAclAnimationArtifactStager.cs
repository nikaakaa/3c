using System;
using System.Collections.Generic;
using System.IO;
using ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation.ACL;
using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal sealed class CharacterAclAnimationArtifactStager : IDisposable
    {
        sealed class BackupEntry
        {
            internal List<string> Paths { get; } = new List<string>();
        }

        readonly string m_OutputFolder;
        readonly string m_FilePrefix;
        readonly string m_StagingFolder;
        readonly string m_BackupFolder;
        readonly List<BackupEntry> m_Backups = new List<BackupEntry>();
        readonly HashSet<string> m_BackupStems =
            new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> m_InstalledStems =
            new HashSet<string>(StringComparer.Ordinal);
        readonly IReadOnlyList<string> m_InterruptedBackupFolders;
        bool m_StagingClosed;
        bool m_Closed;

        internal CharacterAclAnimationArtifactStager(
            string outputFolder,
            IReadOnlyList<string> interruptedBackupFolders)
        {
            m_OutputFolder = outputFolder?.TrimEnd('/') ??
                throw new ArgumentNullException(nameof(outputFolder));
            m_FilePrefix =
                CharacterAclAnimationArtifactIdentity.GetAssetStemPrefix();
            m_InterruptedBackupFolders =
                interruptedBackupFolders ?? Array.Empty<string>();
            CharacterAclAnimationArtifactFileStore.EnsureFolder(m_OutputFolder);
            string transactionId = Guid.NewGuid().ToString("N").Substring(0, 12);
            m_StagingFolder =
                $"{m_OutputFolder}/{CharacterAclAnimationArtifactIdentity.StagingFolderPrefix}{transactionId}";
            m_BackupFolder =
                $"{m_OutputFolder}/{CharacterAclAnimationArtifactIdentity.BackupFolderPrefix}{transactionId}";
            CharacterAclAnimationArtifactFileStore.CreateTransactionFolder(
                m_OutputFolder,
                m_StagingFolder);
        }

        internal string StagingFolder => m_StagingFolder;

        internal CharacterAclAnimationResource StageGroup(
            CharacterAclAnimationPublishGroup group)
        {
            RequireOpen();
            return CharacterAclAnimationArtifactPublisher.PublishGroup(
                group.Artifact,
                new CharacterAclAnimationArtifactPublicationContext(
                    m_StagingFolder,
                    group.AssetStem));
        }

        internal void Backup(
            CharacterAclPublishedGroupInventoryEntry entry)
        {
            RequireOpen();
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));
            if (!m_BackupStems.Add(entry.Stem))
                throw new InvalidOperationException(
                    $"ACL group '{entry.Stem}' was backed up more than once.");
            CharacterAclAnimationArtifactFileStore.EnsureFolder(m_BackupFolder);
            var backup = new BackupEntry();
            m_Backups.Add(backup);
            MovePaths(entry.Paths, m_BackupFolder, backup.Paths);
        }

        internal void Install(string assetStem)
        {
            RequireOpen();
            CharacterAclPublishedGroupInventory inventory =
                CharacterAclPublishedGroupInventory.Scan(
                    m_StagingFolder,
                    m_FilePrefix);
            if (!inventory.TryGet(
                    assetStem,
                    out CharacterAclPublishedGroupInventoryEntry entry))
            {
                throw new InvalidOperationException(
                    $"ACL staged group '{assetStem}' is missing.");
            }
            m_InstalledStems.Add(assetStem);
            MovePaths(entry.Paths, m_OutputFolder, null);
        }

        internal void Complete(IReadOnlyCollection<string> retainedStems)
        {
            RequireOpen();
            if (retainedStems == null)
                throw new ArgumentNullException(nameof(retainedStems));
            CharacterAclPublishedGroupInventory current =
                CharacterAclPublishedGroupInventory.Scan(
                    m_OutputFolder,
                    m_FilePrefix);
            for (int i = 0; i < current.Groups.Count; i++)
            {
                CharacterAclPublishedGroupInventoryEntry entry = current.Groups[i];
                if (!Contains(retainedStems, entry.Stem))
                    Backup(entry);
            }
        }

        internal void CloseStagingFolder()
        {
            RequireOpen();
            if (m_StagingClosed)
                return;
            CharacterAclAnimationArtifactFileStore.DeleteFolder(
                m_StagingFolder,
                CharacterAclAnimationArtifactIdentity.StagingFolderPrefix);
            m_StagingClosed = true;
        }

        internal bool TryFinalizePublication(out string warning)
        {
            RequireOpen();
            var failures = new List<string>();
            CharacterAclAnimationArtifactFileStore.TryDeleteFolder(
                m_StagingFolder,
                CharacterAclAnimationArtifactIdentity.StagingFolderPrefix,
                failures);
            CharacterAclAnimationArtifactFileStore.TryDeleteFolder(
                m_BackupFolder,
                CharacterAclAnimationArtifactIdentity.BackupFolderPrefix,
                failures);
            for (int i = 0; i < m_InterruptedBackupFolders.Count; i++)
            {
                CharacterAclAnimationArtifactFileStore.TryDeleteFolder(
                    m_InterruptedBackupFolders[i],
                    CharacterAclAnimationArtifactIdentity.BackupFolderPrefix,
                    failures);
            }
            warning = failures.Count == 0
                ? string.Empty
                : string.Join(" | ", failures);
            if (failures.Count == 0)
                m_Closed = true;
            return failures.Count == 0;
        }

        internal void Rollback()
        {
            if (m_Closed)
                return;
            Exception failure = null;
            try
            {
                DeleteInstalledGroups();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            bool backupsRestored = true;
            try
            {
                RestoreBackups();
            }
            catch (Exception exception)
            {
                backupsRestored = false;
                failure = failure == null
                    ? exception
                    : new AggregateException(failure, exception);
            }
            try
            {
                CharacterAclAnimationArtifactFileStore.DeleteFolderIfPresent(
                    m_StagingFolder,
                    CharacterAclAnimationArtifactIdentity.StagingFolderPrefix);
                if (backupsRestored)
                {
                    CharacterAclAnimationArtifactFileStore.DeleteFolderIfPresent(
                        m_BackupFolder,
                        CharacterAclAnimationArtifactIdentity.BackupFolderPrefix);
                }
            }
            catch (Exception exception)
            {
                failure = failure == null
                    ? exception
                    : new AggregateException(failure, exception);
            }
            m_Closed = true;
            if (failure != null)
                throw failure;
        }

        void DeleteInstalledGroups()
        {
            CharacterAclPublishedGroupInventory inventory =
                CharacterAclPublishedGroupInventory.Scan(
                    m_OutputFolder,
                    m_FilePrefix);
            foreach (string stem in m_InstalledStems)
            {
                if (!inventory.TryGet(
                        stem,
                        out CharacterAclPublishedGroupInventoryEntry entry))
                    continue;
                var paths = new List<string>(entry.Paths);
                paths.Sort(CompareMovePaths);
                for (int i = paths.Count - 1; i >= 0; i--)
                    CharacterAclAnimationArtifactFileStore.Delete(paths[i]);
            }
        }

        void RestoreBackups()
        {
            for (int i = m_Backups.Count - 1; i >= 0; i--)
                MovePaths(
                    m_Backups[i].Paths,
                    m_OutputFolder,
                    null);
        }

        static void MovePaths(
            IReadOnlyList<string> sourcePaths,
            string destinationFolder,
            List<string> movedPaths)
        {
            var orderedPaths = new List<string>(sourcePaths);
            orderedPaths.Sort(CompareMovePaths);
            for (int i = 0; i < orderedPaths.Count; i++)
            {
                string destinationPath =
                    $"{destinationFolder}/{Path.GetFileName(orderedPaths[i])}";
                CharacterAclAnimationArtifactFileStore.Move(
                    orderedPaths[i],
                    destinationPath);
                movedPaths?.Add(destinationPath);
            }
        }

        static int CompareMovePaths(string left, string right)
        {
            bool leftAsset = left.EndsWith(
                ".asset",
                StringComparison.Ordinal);
            bool rightAsset = right.EndsWith(
                ".asset",
                StringComparison.Ordinal);
            if (leftAsset != rightAsset)
                return leftAsset ? 1 : -1;
            return string.CompareOrdinal(left, right);
        }

        static bool Contains(
            IReadOnlyCollection<string> values,
            string value)
        {
            foreach (string item in values)
            {
                if (string.Equals(item, value, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        void RequireOpen()
        {
            if (m_Closed)
                throw new ObjectDisposedException(
                    nameof(CharacterAclAnimationArtifactStager));
        }

        public void Dispose()
        {
            if (!m_Closed)
                Rollback();
        }
    }
}
