using System;
using System.Collections.Generic;
using System.IO;
using ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation.ACL;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class CharacterAclInterruptedPublicationRecovery
    {
        internal static IReadOnlyList<string> Recover(
            string outputFolder)
        {
            string normalizedOutputFolder = outputFolder?.TrimEnd('/') ??
                throw new ArgumentNullException(nameof(outputFolder));
            string prefix =
                CharacterAclAnimationArtifactIdentity.GetAssetStemPrefix();
            string fullOutputFolder =
                CharacterAclAnimationArtifactFileStore.ToProjectFilePath(
                    normalizedOutputFolder);
            if (!Directory.Exists(fullOutputFolder))
                return Array.Empty<string>();
            string[] transactionFolders = Directory.GetDirectories(
                fullOutputFolder,
                CharacterAclAnimationArtifactIdentity.StagingFolderPrefix + "*",
                SearchOption.TopDirectoryOnly);
            string[] backupFolders = Directory.GetDirectories(
                fullOutputFolder,
                CharacterAclAnimationArtifactIdentity.BackupFolderPrefix + "*",
                SearchOption.TopDirectoryOnly);
            var allTransactionFolders = new List<string>(
                transactionFolders.Length + backupFolders.Length);
            allTransactionFolders.AddRange(transactionFolders);
            allTransactionFolders.AddRange(backupFolders);
            transactionFolders = allTransactionFolders.ToArray();
            Array.Sort(transactionFolders, StringComparer.Ordinal);
            var interruptedBackups = new List<string>();
            for (int i = 0; i < transactionFolders.Length; i++)
            {
                string transactionFolder = transactionFolders[i];
                string name = Path.GetFileName(transactionFolder);
                string transactionAssetFolder =
                    CharacterAclAnimationArtifactFileStore.ToAssetPath(
                        transactionFolder);
                if (name.StartsWith(
                        CharacterAclAnimationArtifactIdentity.StagingFolderPrefix,
                        StringComparison.Ordinal))
                {
                    CharacterAclAnimationArtifactFileStore.DeleteFolderIfPresent(
                        transactionAssetFolder,
                        CharacterAclAnimationArtifactIdentity.StagingFolderPrefix);
                    continue;
                }
                if (!name.StartsWith(
                        CharacterAclAnimationArtifactIdentity.BackupFolderPrefix,
                        StringComparison.Ordinal))
                    continue;
                RecoverBackup(
                    transactionAssetFolder,
                    normalizedOutputFolder,
                    prefix);
                if (IsEmpty(transactionFolder))
                {
                    CharacterAclAnimationArtifactFileStore.DeleteFolderIfPresent(
                        transactionAssetFolder,
                        CharacterAclAnimationArtifactIdentity.BackupFolderPrefix);
                }
                else
                    interruptedBackups.Add(transactionAssetFolder);
            }
            return interruptedBackups;
        }

        static void RecoverBackup(
            string backupFolder,
            string outputFolder,
            string filePrefix)
        {
            CharacterAclPublishedGroupInventory backupInventory =
                CharacterAclPublishedGroupInventory.Scan(
                    backupFolder,
                    filePrefix);
            CharacterAclPublishedGroupInventory currentInventory =
                CharacterAclPublishedGroupInventory.Scan(
                    outputFolder,
                    filePrefix);
            for (int groupIndex = 0;
                 groupIndex < backupInventory.Groups.Count;
                 groupIndex++)
            {
                CharacterAclPublishedGroupInventoryEntry group =
                    backupInventory.Groups[groupIndex];
                if (group.State != CharacterAclPublishedGroupFileState.Complete)
                    continue;
                if (currentInventory.TryGet(
                        group.Stem,
                        out CharacterAclPublishedGroupInventoryEntry currentGroup))
                {
                    if (currentGroup.State ==
                        CharacterAclPublishedGroupFileState.Complete)
                        continue;
                    DeletePaths(currentGroup.Paths);
                    currentInventory = CharacterAclPublishedGroupInventory.Scan(
                        outputFolder,
                        filePrefix);
                    if (currentInventory.TryGet(group.Stem, out _))
                        throw new InvalidOperationException(
                            $"ACL incomplete current group '{group.Stem}' could not be removed during recovery.");
                }
                MovePaths(group.Paths, outputFolder);
                currentInventory = CharacterAclPublishedGroupInventory.Scan(
                    outputFolder,
                    filePrefix);
                if (!currentInventory.TryGet(
                        group.Stem,
                        out CharacterAclPublishedGroupInventoryEntry restored) ||
                    restored.State != CharacterAclPublishedGroupFileState.Complete)
                {
                    throw new InvalidOperationException(
                        $"ACL backup group '{group.Stem}' could not be restored during recovery.");
                }
            }
        }

        static void MovePaths(
            IReadOnlyList<string> sourcePaths,
            string destinationFolder)
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
            }
        }

        static void DeletePaths(IReadOnlyList<string> paths)
        {
            var orderedPaths = new List<string>(paths);
            orderedPaths.Sort(CompareMovePaths);
            for (int i = orderedPaths.Count - 1; i >= 0; i--)
                CharacterAclAnimationArtifactFileStore.Delete(orderedPaths[i]);
            CharacterAclAnimationArtifactFileStore.Refresh();
            for (int i = 0; i < orderedPaths.Count; i++)
            {
                if (CharacterAclAnimationArtifactFileStore.LogicalPathExists(
                        orderedPaths[i]))
                {
                    throw new InvalidOperationException(
                        $"ACL incomplete current group file remained after recovery delete: {orderedPaths[i]}.");
                }
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

        static bool IsEmpty(string folder) =>
            Directory.GetFiles(
                folder,
                "*",
                SearchOption.TopDirectoryOnly).Length == 0 &&
            Directory.GetDirectories(
                folder,
                "*",
                SearchOption.TopDirectoryOnly).Length == 0;
    }
}
