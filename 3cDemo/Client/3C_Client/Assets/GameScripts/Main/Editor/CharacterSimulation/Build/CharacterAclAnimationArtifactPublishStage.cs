using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation;
using ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation.ACL;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal sealed class CharacterAclAnimationPublishGroup
    {
        internal CharacterAclAnimationPublishGroup(
            CharacterAclAnimationGroupArtifact artifact,
            CharacterAnimationCompiledResourceDescriptor descriptor,
            string assetStem)
        {
            Artifact = artifact ?? throw new ArgumentNullException(nameof(artifact));
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            AssetStem = assetStem ?? throw new ArgumentNullException(nameof(assetStem));
        }

        internal CharacterAclAnimationGroupArtifact Artifact { get; }
        internal CharacterAnimationCompiledResourceDescriptor Descriptor { get; }
        internal string AssetStem { get; }
        internal CharacterAclAnimationResource Resource { get; set; }
    }

    internal sealed class CharacterAclAnimationArtifactPublishStage :
        ICharacterSimulationTargetPublishStage,
        ICharacterSimulationTargetPublishFinalizer
    {
        readonly string m_OwnerAssetGuid;
        readonly string m_OutputFolder;
        readonly string m_FilePrefix;
        readonly CharacterAclPublishedGroupInventory m_Inventory;
        readonly IReadOnlyList<string> m_InterruptedBackupFolders;
        readonly List<CharacterAclAnimationPublishGroup> m_Publications =
            new List<CharacterAclAnimationPublishGroup>();
        readonly HashSet<string> m_ResolvedStems =
            new HashSet<string>(StringComparer.Ordinal);
        CharacterAclAnimationArtifactStager m_Stager;
        bool m_Committed;
        bool m_Completed;
        bool m_PublicationAccepted;

        internal CharacterAclAnimationArtifactPublishStage(
            string ownerAssetGuid,
            CharacterAnimationBuildCatalog catalog)
        {
            m_OwnerAssetGuid =
                CharacterAclAnimationArtifactIdentity.RequireOwnerAssetGuid(
                    ownerAssetGuid);
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            m_OutputFolder =
                CharacterAclAnimationArtifactIdentity.GetOutputFolder(
                    m_OwnerAssetGuid);
            m_FilePrefix =
                CharacterAclAnimationArtifactIdentity.GetAssetStemPrefix();
            m_InterruptedBackupFolders =
                CharacterAclInterruptedPublicationRecovery.Recover(
                    m_OutputFolder);
            IReadOnlyList<CharacterAclAnimationGroupArtifact> groups =
                catalog.AnimationArtifacts;
            if (catalog.AnimationResources.Count != groups.Count)
                throw new InvalidOperationException(
                    "ACL animation resource descriptor and group artifact counts differ.");
            for (int i = 0; i < groups.Count; i++)
            {
                CharacterAclAnimationGroupArtifact group = groups[i] ??
                    throw new InvalidOperationException(
                        "ACL animation group artifact is missing.");
                CharacterAnimationCompiledResourceDescriptor descriptor =
                    FindDescriptor(catalog.AnimationResources, group.GroupIndex);
                descriptor.RequireValid();
                if (descriptor.GroupIndex != group.GroupIndex ||
                    !string.Equals(
                        descriptor.GroupContentHash,
                        group.GroupContentHash,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "ACL animation group artifact does not match its compiled resource descriptor.");
                }
                string assetStem =
                    CharacterAclAnimationArtifactIdentity.GetAssetStem(
                        group.GroupContentHash);
                if (!string.Equals(
                        descriptor.ResourceAddress,
                        assetStem,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        group.Manifests[0].ResourceAddress,
                        assetStem,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "ACL animation resource address does not match its formal asset filename.");
                }
                m_Publications.Add(
                    new CharacterAclAnimationPublishGroup(
                        group,
                        descriptor,
                        assetStem));
            }
            m_Inventory =
                CharacterAclPublishedGroupInventory.Scan(
                    m_OutputFolder,
                    m_FilePrefix);
        }

        public UnityEngine.Object Wrapper
        {
            get
            {
                for (int i = 0; i < m_Publications.Count; i++)
                {
                    if (m_Publications[i].Resource)
                        return m_Publications[i].Resource;
                }
                return null;
            }
        }

        public void Commit()
        {
            if (m_Committed)
                throw new InvalidOperationException(
                    "ACL animation artifact publication is already committed.");
            m_Committed = true;
            try
            {
                m_Stager =
                    new CharacterAclAnimationArtifactStager(
                        m_OutputFolder,
                        m_InterruptedBackupFolders);
                for (int i = 0; i < m_Publications.Count; i++)
                    CommitGroup(m_Publications[i]);
                m_Stager.CloseStagingFolder();
            }
            catch
            {
                Rollback();
                throw;
            }
        }

        public void Complete()
        {
            if (!m_Committed || m_Completed)
                throw new InvalidOperationException(
                    "ACL animation artifact publication cannot complete before commit.");
            var retainedStems = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < m_Publications.Count; i++)
                retainedStems.Add(m_Publications[i].AssetStem);
            m_Stager.Complete(retainedStems);
            m_Completed = true;
        }

        bool ICharacterSimulationTargetPublishFinalizer.TryFinalizePublication(
            out string warning)
        {
            warning = string.Empty;
            if (!m_Committed || !m_Completed || m_PublicationAccepted)
            {
                warning =
                    "ACL animation publication is not in a finalizable state.";
                return false;
            }
            m_PublicationAccepted = true;
            bool success;
            try
            {
                success = m_Stager.TryFinalizePublication(out warning);
            }
            catch (Exception exception)
            {
                success = false;
                warning = exception.Message;
            }
            finally
            {
                m_Committed = false;
            }
            return success;
        }

        public void Rollback()
        {
            if (!m_Committed || m_PublicationAccepted)
                return;
            try
            {
                m_Stager?.Rollback();
            }
            finally
            {
                m_Committed = false;
            }
        }

        void CommitGroup(CharacterAclAnimationPublishGroup group)
        {
            if (m_ResolvedStems.Contains(group.AssetStem))
            {
                CharacterAclPublishedGroupInventory current =
                    CharacterAclPublishedGroupInventory.Scan(
                        m_OutputFolder,
                        m_FilePrefix);
                if (!current.TryGet(
                        group.AssetStem,
                        out CharacterAclPublishedGroupInventoryEntry entry))
                {
                    throw new InvalidOperationException(
                        $"ACL resolved group '{group.AssetStem}' disappeared before validation.");
                }
                CharacterAclAnimationGroupArtifactValidator.Validate(
                    group,
                    m_OutputFolder,
                    entry);
                StampBuildInputIdentity(entry, group);
                group.Resource = entry.Resource;
                return;
            }
            if (m_Inventory.TryGet(
                    group.AssetStem,
                    out CharacterAclPublishedGroupInventoryEntry existing))
            {
                if (CharacterAclAnimationGroupArtifactValidator.TryReuse(
                        group,
                        m_OutputFolder,
                        existing))
                {
                    StampBuildInputIdentity(existing, group);
                    group.Resource = existing.Resource;
                    m_ResolvedStems.Add(group.AssetStem);
                    return;
                }
                m_Stager.Backup(existing);
            }
            m_Stager.StageGroup(group);
            CharacterAclPublishedGroupInventory stagedInventory =
                CharacterAclPublishedGroupInventory.Scan(
                    m_Stager.StagingFolder,
                    m_FilePrefix);
            if (!stagedInventory.TryGet(
                    group.AssetStem,
                    out CharacterAclPublishedGroupInventoryEntry stagedEntry))
            {
                throw new InvalidOperationException(
                    $"ACL staged group '{group.AssetStem}' was not inventoried.");
            }
            CharacterAclAnimationGroupArtifactValidator.Validate(
                group,
                m_Stager.StagingFolder,
                stagedEntry);
            m_Stager.Install(group.AssetStem);
            CharacterAclPublishedGroupInventory finalInventory =
                CharacterAclPublishedGroupInventory.Scan(
                    m_OutputFolder,
                    m_FilePrefix);
            if (!finalInventory.TryGet(
                    group.AssetStem,
                    out CharacterAclPublishedGroupInventoryEntry finalEntry))
            {
                throw new InvalidOperationException(
                    $"ACL installed group '{group.AssetStem}' was not inventoried.");
            }
            CharacterAclAnimationGroupArtifactValidator.Validate(
                group,
                m_OutputFolder,
                finalEntry);
            group.Resource = finalEntry.Resource;
            m_ResolvedStems.Add(group.AssetStem);
        }

        void StampBuildInputIdentity(
            CharacterAclPublishedGroupInventoryEntry entry,
            CharacterAclAnimationPublishGroup group)
        {
            CharacterAclAnimationResource resource = entry.Resource;
            if (resource == null ||
                string.Equals(
                    resource.BuildInputIdentity,
                    group.Artifact.BuildInputIdentity,
                    StringComparison.Ordinal))
            {
                return;
            }
            resource.StampBuildInputIdentity(group.Artifact.BuildInputIdentity);
            EditorUtility.SetDirty(resource);
            AssetDatabase.SaveAssetIfDirty(resource);
            AssetDatabase.ImportAsset(
                $"{entry.Folder}/{entry.Stem}.asset",
                ImportAssetOptions.ForceSynchronousImport);
        }

        static CharacterAnimationCompiledResourceDescriptor FindDescriptor(
            IReadOnlyList<CharacterAnimationCompiledResourceDescriptor> descriptors,
            int groupIndex)
        {
            for (int i = 0; i < descriptors.Count; i++)
            {
                CharacterAnimationCompiledResourceDescriptor descriptor =
                    descriptors[i];
                if (descriptor != null && descriptor.GroupIndex == groupIndex)
                    return descriptor;
            }
            throw new InvalidOperationException(
                "ACL animation group resource descriptor is missing.");
        }

        public void Dispose()
        {
            if (m_Committed && !m_PublicationAccepted)
                Rollback();
        }
    }
}
