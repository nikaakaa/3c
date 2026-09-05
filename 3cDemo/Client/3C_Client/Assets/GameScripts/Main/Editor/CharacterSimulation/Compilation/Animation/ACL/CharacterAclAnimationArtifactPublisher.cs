using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation.ACL
{
    internal sealed class CharacterAclAnimationArtifactPublicationContext
    {
        internal CharacterAclAnimationArtifactPublicationContext(
            string outputFolder,
            string assetStem)
        {
            if (string.IsNullOrWhiteSpace(outputFolder) ||
                (!string.Equals(outputFolder, "Assets", StringComparison.Ordinal) &&
                 !outputFolder.StartsWith("Assets/", StringComparison.Ordinal)) ||
                string.IsNullOrWhiteSpace(assetStem) ||
                assetStem.IndexOf('/') >= 0 ||
                assetStem.IndexOf('\\') >= 0)
            {
                throw new ArgumentException("ACL artifact publication context is invalid.");
            }
            OutputFolder = outputFolder.TrimEnd('/');
            AssetStem = assetStem.Trim();
        }

        internal string OutputFolder { get; }
        internal string AssetStem { get; }
    }

    internal static class CharacterAclAnimationArtifactPublisher
    {
        internal static CharacterAclAnimationResource PublishGroup(
            CharacterAclAnimationGroupArtifact artifact,
            CharacterAclAnimationArtifactPublicationContext context)
        {
            if (artifact == null)
                throw new ArgumentNullException(nameof(artifact));
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            CharacterAclAnimationResourceManifest[] groupManifests = artifact.Manifests;
            byte[][] groupTransformPayloads = artifact.TransformPayloads;
            byte[][] groupScalarPayloads = artifact.ScalarPayloads;
            string stem = context.AssetStem;
            if (!string.Equals(
                    groupManifests[0].ResourceAddress,
                    stem,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "ACL resource manifest address does not match its asset filename.");
            }
            CharacterAclAnimationArtifactFileStore.EnsureFolder(
                context.OutputFolder);
            string[] transformPaths = new string[groupManifests.Length];
            string[] scalarPaths = new string[groupManifests.Length];
            for (int i = 0; i < groupManifests.Length; i++)
            {
                transformPaths[i] = $"{context.OutputFolder}/{stem}.transform.{i}.bytes";
                scalarPaths[i] = $"{context.OutputFolder}/{stem}.scalar.{i}.bytes";
            }
            string databasePath = $"{context.OutputFolder}/{stem}.database.bytes";
            string mediumPath = $"{context.OutputFolder}/{stem}.medium.bytes";
            string lowPath = $"{context.OutputFolder}/{stem}.low.bytes";
            string manifestPath = $"{context.OutputFolder}/{stem}.asset";
            string reportPath = $"{context.OutputFolder}/{stem}.quality.json";
            var outputPaths = new List<string>(groupManifests.Length * 2 + 5);
            for (int i = 0; i < transformPaths.Length; i++)
            {
                outputPaths.Add(transformPaths[i]);
                outputPaths.Add(scalarPaths[i]);
            }
            outputPaths.Add(databasePath);
            outputPaths.Add(mediumPath);
            outputPaths.Add(lowPath);
            outputPaths.Add(manifestPath);
            outputPaths.Add(reportPath);
            for (int i = 0; i < outputPaths.Count; i++)
            {
                if (CharacterAclAnimationArtifactFileStore.Exists(outputPaths[i]))
                    throw new InvalidOperationException($"ACL build output already exists: {stem}.");
            }

            for (int i = 0; i < groupManifests.Length; i++)
            {
                WritePayload(transformPaths[i], groupTransformPayloads[i]);
                WritePayload(scalarPaths[i], groupScalarPayloads[i]);
            }
            WritePayload(databasePath, artifact.DatabaseHeaderPayload);
            WritePayload(mediumPath, artifact.BulkMediumPayload);
            WritePayload(lowPath, artifact.BulkLowPayload);
            string report = JsonUtility.ToJson(
                new CharacterAclAnimationQualityBundle(artifact.QualityReports),
                true);
            CharacterAclAnimationArtifactFileStore.WriteAllTextUtf8(
                reportPath,
                report);
            for (int i = 0; i < groupManifests.Length; i++)
            {
                ImportIfPresent(transformPaths[i], groupTransformPayloads[i]);
                ImportIfPresent(scalarPaths[i], groupScalarPayloads[i]);
            }
            ImportIfPresent(databasePath, artifact.DatabaseHeaderPayload);
            ImportIfPresent(mediumPath, artifact.BulkMediumPayload);
            ImportIfPresent(lowPath, artifact.BulkLowPayload);
            AssetDatabase.ImportAsset(reportPath, ImportAssetOptions.ForceSynchronousImport);
            var transformPayloads = new TextAsset[groupManifests.Length];
            var scalarPayloads = new TextAsset[groupManifests.Length];
            for (int i = 0; i < groupManifests.Length; i++)
            {
                transformPayloads[i] = RequireTextAsset(transformPaths[i]);
                scalarPayloads[i] = LoadOptional(scalarPaths[i], groupScalarPayloads[i]);
            }
            TextAsset databasePayload = LoadOptional(databasePath, artifact.DatabaseHeaderPayload);
            TextAsset mediumPayload = LoadOptional(mediumPath, artifact.BulkMediumPayload);
            TextAsset lowPayload = LoadOptional(lowPath, artifact.BulkLowPayload);
            var resource = ScriptableObject.CreateInstance<CharacterAclAnimationResource>();
            resource.name = stem;
            AssetDatabase.CreateAsset(resource, manifestPath);
            resource.ConfigureForBuild(
                groupManifests[0],
                groupManifests,
                transformPayloads,
                scalarPayloads,
                databasePayload,
                mediumPayload,
                lowPayload);
            EditorUtility.SetDirty(resource);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(manifestPath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<CharacterAclAnimationResource>(manifestPath) ??
                throw new InvalidOperationException(
                    $"ACL resource asset '{manifestPath}' was not imported.");
        }

        static void WritePayload(string assetPath, byte[] payload)
        {
            if (payload != null && payload.Length > 0)
                CharacterAclAnimationArtifactFileStore.WriteAllBytes(
                    assetPath,
                    payload);
        }

        static void ImportIfPresent(string assetPath, byte[] payload)
        {
            if (payload != null && payload.Length > 0)
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        static TextAsset LoadOptional(string assetPath, byte[] payload) =>
            payload != null && payload.Length > 0
                ? RequireTextAsset(assetPath)
                : null;

        static TextAsset RequireTextAsset(string assetPath) =>
            AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath) ??
            throw new InvalidOperationException(
                $"ACL payload asset '{assetPath}' was not imported.");

    }
}
