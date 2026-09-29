using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation;
using ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation.ACL;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class CharacterAclAnimationPublicationMenu
    {
        [MenuItem("3C/Character/Animation/Publish Selected Definition ACL Resources")]
        public static void PublishSelected()
        {
            Publish(Selection.activeObject as CharacterPipelineDefinition);
        }

        [MenuItem("3C/Character/Animation/Corin/Publish ACL Resources")]
        public static void PublishCorin()
        {
            Publish(AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(
                "Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset"));
        }

        static void Publish(CharacterPipelineDefinition definition)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("ACL publication requires an idle Editor outside Play mode.");
            if (!definition)
                throw new InvalidOperationException("Select a Character Pipeline Definition to publish its ACL resources.");
            CharacterAnimationBuildInput input = CharacterAnimationBuildInputFactory.Create(definition);
            var errors = new List<string>();
            CharacterAnimationBuildInputFactory.RegisterDeclaredSources(input, errors);
            if (errors.Count != 0)
                throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
            IReadOnlyList<CharacterAclAnimationGroupArtifact> artifacts = input.AnimationCatalog.BuildAclArtifacts();
            if (artifacts.Count == 0)
                throw new InvalidOperationException("The selected Definition has no declared ACL sources.");
            string outputFolder = CharacterAclAnimationArtifactIdentity.GetOutputFolder(input.OwnerAssetGuid);
            IReadOnlyList<string> recovered = CharacterAclInterruptedPublicationRecovery.Recover(outputFolder);
            var groups = new List<CharacterAclAnimationPublishGroup>(artifacts.Count);
            var retained = new HashSet<string>(StringComparer.Ordinal);
            using (var publication = new CharacterAclAnimationArtifactStager(outputFolder, recovered))
            {
                for (int i = 0; i < artifacts.Count; i++)
                {
                    CharacterAclAnimationGroupArtifact artifact = artifacts[i];
                    string stem = CharacterAclAnimationArtifactIdentity.GetAssetStem(artifact.GroupContentHash);
                    var group = new CharacterAclAnimationPublishGroup(artifact, artifact.CreateDescriptor(i), stem);
                    group.Resource = publication.StageGroup(group);
                    groups.Add(group);
                    retained.Add(stem);
                }
                CharacterAclPublishedGroupInventory staged = CharacterAclPublishedGroupInventory.Scan(
                    publication.StagingFolder, CharacterAclAnimationArtifactIdentity.GetAssetStemPrefix());
                CharacterAclPublishedGroupInventory current = CharacterAclPublishedGroupInventory.Scan(
                    outputFolder, CharacterAclAnimationArtifactIdentity.GetAssetStemPrefix());
                for (int i = 0; i < groups.Count; i++)
                {
                    CharacterAclAnimationPublishGroup group = groups[i];
                    staged.TryGet(group.AssetStem, out CharacterAclPublishedGroupInventoryEntry candidate);
                    CharacterAclAnimationGroupArtifactValidator.Validate(group, publication.StagingFolder, candidate);
                    if (current.TryGet(group.AssetStem, out CharacterAclPublishedGroupInventoryEntry previous))
                        publication.Backup(previous);
                    publication.Install(group.AssetStem);
                }
                CharacterAclPublishedGroupInventory installed = CharacterAclPublishedGroupInventory.Scan(
                    outputFolder, CharacterAclAnimationArtifactIdentity.GetAssetStemPrefix());
                for (int i = 0; i < groups.Count; i++)
                {
                    installed.TryGet(groups[i].AssetStem, out CharacterAclPublishedGroupInventoryEntry entry);
                    CharacterAclAnimationGroupArtifactValidator.Validate(groups[i], outputFolder, entry);
                }
                publication.Complete(retained);
                if (!publication.TryFinalizePublication(out string warning))
                    throw new InvalidOperationException(warning);
            }
            Debug.Log($"[ACL] Published {groups.Count} resource groups for '{definition.name}'. Compile Animation Domain Resources to adopt them.");
        }
    }
}
