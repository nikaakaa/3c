using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.AnimationImport
{
    public static class ExternalHumanoidAnimationConverter
    {
        const string MenuPath = "Tools/Character Animation/Convert ALS and GASP to Humanoid";

        static readonly SourceDefinition[] Sources =
        {
            new(
                "ALS",
                "Assets/AssetArt/Animation/als",
                "Assets/AssetArt/Animation/als/Base/Locomotion/ALS_N_Walk_F.fbx"),
            new(
                "GASP",
                "Assets/AssetArt/Animation/gasp",
                "Assets/AssetArt/Animation/gasp/Idle/M_Neutral_Stand_Idle_Loop.fbx")
        };

        [MenuItem(MenuPath)]
        public static void ConvertAll()
        {
            try
            {
                SourceContext[] contexts = Sources.Select(CreateSourceContext).ToArray();
                int total = contexts.Sum(context => context.AssetPaths.Count);
                int completed = 0;

                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (SourceContext context in contexts)
                    {
                        foreach (string assetPath in context.AssetPaths)
                        {
                            completed++;
                            EditorUtility.DisplayProgressBar(
                                "Convert ALS and GASP to Humanoid",
                                $"{context.Definition.Name}: {assetPath}",
                                completed / (float)total);

                            if (assetPath == context.Definition.AvatarSourcePath)
                                continue;

                            ModelImporter importer = RequireModelImporter(assetPath);
                            importer.importAnimation = true;
                            importer.animationType = ModelImporterAnimationType.Human;
                            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                            importer.sourceAvatar = context.Avatar;
                            importer.SaveAndReimport();
                        }
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }

                AssetDatabase.Refresh();
                ValidateConvertedAssets(contexts);
                Debug.Log($"Converted {total} ALS and GASP FBX assets to Humanoid.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        static SourceContext CreateSourceContext(SourceDefinition definition)
        {
            List<string> assetPaths = FindFbxAssets(definition.RootPath);
            if (assetPaths.Count == 0)
                throw new InvalidOperationException($"{definition.Name} contains no FBX assets: {definition.RootPath}");
            if (!assetPaths.Contains(definition.AvatarSourcePath))
                throw new InvalidOperationException($"{definition.Name} avatar source is missing: {definition.AvatarSourcePath}");

            ModelImporter importer = RequireModelImporter(definition.AvatarSourcePath);
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.sourceAvatar = null;
            importer.SaveAndReimport();

            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(definition.AvatarSourcePath)
                .OfType<Avatar>()
                .SingleOrDefault();
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
                throw new InvalidOperationException(
                    $"{definition.Name} could not generate a valid Humanoid Avatar from {definition.AvatarSourcePath}");

            return new SourceContext(definition, assetPaths, avatar);
        }

        static List<string> FindFbxAssets(string rootPath)
        {
            return AssetDatabase.FindAssets("t:Model", new[] { rootPath })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => string.Equals(Path.GetExtension(path), ".fbx", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();
        }

        static ModelImporter RequireModelImporter(string assetPath)
        {
            if (AssetImporter.GetAtPath(assetPath) is ModelImporter importer)
                return importer;
            throw new InvalidOperationException($"FBX has no ModelImporter: {assetPath}");
        }

        static void ValidateConvertedAssets(IEnumerable<SourceContext> contexts)
        {
            List<string> invalidAssets = new();
            foreach (SourceContext context in contexts)
            {
                foreach (string assetPath in context.AssetPaths)
                {
                    ModelImporter importer = RequireModelImporter(assetPath);
                    bool isAvatarSource = assetPath == context.Definition.AvatarSourcePath;
                    bool validSetup = isAvatarSource
                        ? importer.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel
                        : importer.avatarSetup == ModelImporterAvatarSetup.CopyFromOther &&
                          importer.sourceAvatar == context.Avatar;
                    if (importer.animationType != ModelImporterAnimationType.Human || !validSetup)
                    {
                        invalidAssets.Add(assetPath);
                    }
                }
            }

            if (invalidAssets.Count > 0)
                throw new InvalidOperationException(
                    $"Humanoid conversion failed for {invalidAssets.Count} FBX assets:\n{string.Join("\n", invalidAssets)}");
        }

        readonly struct SourceDefinition
        {
            public SourceDefinition(string name, string rootPath, string avatarSourcePath)
            {
                Name = name;
                RootPath = rootPath;
                AvatarSourcePath = avatarSourcePath;
            }

            public string Name { get; }
            public string RootPath { get; }
            public string AvatarSourcePath { get; }
        }

        sealed class SourceContext
        {
            public SourceContext(SourceDefinition definition, List<string> assetPaths, Avatar avatar)
            {
                Definition = definition;
                AssetPaths = assetPaths;
                Avatar = avatar;
            }

            public SourceDefinition Definition { get; }
            public List<string> AssetPaths { get; }
            public Avatar Avatar { get; }
        }
    }
}
