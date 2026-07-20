using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    [InitializeOnLoad]
    public static class CharacterSimulationProgramBuildService
    {
        static bool s_Building;
        static bool s_Scheduled;
        static bool s_CheckSourceRevision;

        static CharacterSimulationProgramBuildService()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            ScheduleBuildAllStale(false);
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode && s_CheckSourceRevision)
                ScheduleBuildAllStale();
        }

        [MenuItem("Assets/3C/Compile Character Simulation Program", true)]
        static bool CanCompileSelected()
        {
            return Selection.objects.OfType<CharacterPipelineDefinition>().Any();
        }

        [MenuItem("Assets/3C/Compile Character Simulation Program")]
        static void CompileSelected()
        {
            foreach (CharacterPipelineDefinition definition in Selection.objects.OfType<CharacterPipelineDefinition>())
                Build(definition, true);
        }

        public static bool Build(CharacterPipelineDefinition definition, bool logReport)
        {
            if (!definition || s_Building)
                return false;
            s_Building = true;
            try
            {
                CharacterSimulationBuildResult result = CharacterSimulationBuildOrchestrator.Build(definition);
                if (!result.IsValid)
                {
                    if (logReport)
                        LogReport(definition, result.Report);
                    return false;
                }
                if (logReport)
                    LogReport(definition, result.Report);
                return true;
            }
            finally
            {
                s_Building = false;
            }
        }

        internal static IDisposable RetainAuthoringDependenciesForPlayerBuild()
        {
            return AuthoringBuildRetention.Acquire();
        }

        internal static void ScheduleBuildAllStale(bool checkSourceRevision = true)
        {
            s_CheckSourceRevision |= checkSourceRevision;
            if (s_Scheduled)
                return;
            s_Scheduled = true;
            EditorApplication.delayCall += () =>
            {
                s_Scheduled = false;
                if (EditorApplication.isPlayingOrWillChangePlaymode || s_Building)
                    return;
                bool inspectSourceRevision = s_CheckSourceRevision;
                s_CheckSourceRevision = false;
                string[] guids = AssetDatabase.FindAssets("t:CharacterPipelineDefinition");
                Array.Sort(guids, StringComparer.Ordinal);
                for (int i = 0; i < guids.Length; i++)
                {
                    CharacterPipelineDefinition definition = AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(AssetDatabase.GUIDToAssetPath(guids[i]));
                    if (definition && (inspectSourceRevision ? IsStale(definition) : !HasPublishedArtifactMetadata(definition)))
                        Build(definition, true);
                }
            };
        }

        public static bool IsStale(CharacterPipelineDefinition definition)
        {
            CharacterSimulationProgramAsset asset = definition.SimulationProgram;
            CharacterPresentationProjectionAsset projection = definition.PresentationProjection;
            if (!asset || !projection ||
                !string.Equals(asset.CompilerVersion, CharacterSemanticFrontendCompiler.CompilerVersion, StringComparison.Ordinal))
                return true;
            try
            {
                ProgramId expectedProgramId = CharacterSemanticFrontendCompiler.ComputeProgramId(definition);
                ProgramRevision revision = CharacterSemanticFrontendCompiler.ComputeSourceRevision(definition);
                return !string.Equals(asset.ProgramId, expectedProgramId.Value, StringComparison.Ordinal) ||
                       !string.Equals(projection.ProgramId, expectedProgramId.Value, StringComparison.Ordinal) ||
                       !string.Equals(asset.SourceRevision, revision.Value, StringComparison.Ordinal) ||
                       !string.Equals(projection.SourceRevision, revision.Value, StringComparison.Ordinal) ||
                       !string.Equals(projection.ProgramHash, asset.ProgramHash, StringComparison.Ordinal) ||
                       !HasCurrentTargetArtifact(definition, asset);
            }
            catch
            {
                return true;
            }
        }

        public static bool HasCurrentArtifactMetadata(CharacterPipelineDefinition definition)
        {
            if (!HasPublishedArtifactMetadata(definition))
                return false;
            CharacterSimulationProgramAsset asset = definition.SimulationProgram;
            try
            {
                return HasCurrentTargetArtifact(definition, asset);
            }
            catch
            {
                return false;
            }
        }

        public static bool HasPublishedArtifactMetadata(CharacterPipelineDefinition definition)
        {
            if (!definition)
                return false;
            CharacterSimulationProgramAsset asset = definition.SimulationProgram;
            CharacterPresentationProjectionAsset projection = definition.PresentationProjection;
            if (!asset || !projection ||
                !string.Equals(asset.CompilerVersion, CharacterSemanticFrontendCompiler.CompilerVersion, StringComparison.Ordinal) ||
                !string.Equals(asset.OperationSetVersion, CharacterSemanticFrontendCompiler.OperationSetVersion.Value, StringComparison.Ordinal) ||
                string.IsNullOrEmpty(asset.SourceRevision) ||
                string.IsNullOrEmpty(asset.SemanticHash) ||
                string.IsNullOrEmpty(asset.ProgramHash) ||
                string.IsNullOrEmpty(asset.LayoutHash) ||
                string.IsNullOrEmpty(asset.CanonicalBytesHash))
                return false;
            try
            {
                string programId = CharacterSemanticFrontendCompiler.ComputeProgramId(definition).Value;
                return string.Equals(asset.ProgramId, programId, StringComparison.Ordinal) &&
                       string.Equals(projection.ProgramId, programId, StringComparison.Ordinal) &&
                       string.Equals(projection.ProgramHash, asset.ProgramHash, StringComparison.Ordinal) &&
                       string.Equals(projection.SourceRevision, asset.SourceRevision, StringComparison.Ordinal) &&
                       string.Equals(projection.SemanticHash, asset.SemanticHash, StringComparison.Ordinal) &&
                       string.Equals(projection.NumericProfileId, asset.NumericProfileId, StringComparison.Ordinal) &&
                       projection.TargetAbiVersion == asset.TargetAbiVersion;
            }
            catch
            {
                return false;
            }
        }

        static void LogReport(CharacterPipelineDefinition definition, CharacterSimulationCompileReport report)
        {
            for (int i = 0; i < report.Messages.Count; i++)
            {
                CharacterSimulationCompileMessage message = report.Messages[i];
                string text = $"Character Simulation compile {message}";
                if (message.Severity == CharacterSimulationCompileSeverity.Error)
                    Debug.LogError(text, definition);
                else if (message.Severity == CharacterSimulationCompileSeverity.Warning)
                    Debug.LogWarning(text, definition);
                else
                    Debug.Log(text, definition);
            }
        }

        static bool HasCurrentTargetArtifact(
            CharacterPipelineDefinition definition,
            CharacterSimulationProgramAsset asset)
        {
            CharacterSimulationProgram program = asset.Load();
            string definitionGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(definition));
            CharacterTargetProgramArtifactExpectation expectation =
                CharacterTargetProgramArtifactStore.CreateExpectation(definitionGuid, program);
            CharacterTargetProgramArtifactResult result =
                CharacterTargetProgramArtifactStore.LoadCurrent(definitionGuid, expectation);
            if (!result.IsCurrent)
                return false;
            CharacterTargetProgramArtifactDescriptor descriptor = result.Artifact.Descriptor;
            if (!string.Equals(descriptor.CanonicalBytesHash.ToString(), asset.CanonicalBytesHash, StringComparison.Ordinal) ||
                descriptor.CanonicalByteLength != asset.CanonicalByteLength)
                return false;
            byte[] stored = result.Artifact.CopyCanonicalBytes();
            byte[] embedded = asset.CopyCanonicalArtifact();
            if (stored.Length != embedded.Length)
                return false;
            for (int i = 0; i < stored.Length; i++)
            {
                if (stored[i] != embedded[i])
                    return false;
            }
            return true;
        }

        sealed class AuthoringBuildRetention : IDisposable
        {
            static UnityEngine.Object[] s_RetainedAssets = Array.Empty<UnityEngine.Object>();
            static bool s_IsAcquired;

            bool m_IsDisposed;

            AuthoringBuildRetention()
            {
            }

            public static AuthoringBuildRetention Acquire()
            {
                if (s_IsAcquired)
                    throw new InvalidOperationException("Character Simulation authoring build retention is already active.");

                string[] definitionGuids = AssetDatabase.FindAssets($"t:{nameof(CharacterPipelineDefinition)}");
                Array.Sort(definitionGuids, StringComparer.Ordinal);

                var retainedAssets = new List<UnityEngine.Object>();
                var retainedInstanceIds = new HashSet<int>();
                for (int i = 0; i < definitionGuids.Length; i++)
                {
                    string definitionPath = AssetDatabase.GUIDToAssetPath(definitionGuids[i]);
                    string[] dependencyPaths = AssetDatabase.GetDependencies(definitionPath, true);
                    Array.Sort(dependencyPaths, StringComparer.Ordinal);
                    for (int dependencyIndex = 0; dependencyIndex < dependencyPaths.Length; dependencyIndex++)
                    {
                        UnityEngine.Object[] dependencyAssets = AssetDatabase.LoadAllAssetsAtPath(dependencyPaths[dependencyIndex]);
                        for (int assetIndex = 0; assetIndex < dependencyAssets.Length; assetIndex++)
                        {
                            UnityEngine.Object asset = dependencyAssets[assetIndex];
                            if (asset && retainedInstanceIds.Add(asset.GetInstanceID()))
                                retainedAssets.Add(asset);
                        }
                    }
                }

                s_RetainedAssets = retainedAssets.ToArray();
                s_IsAcquired = true;
                return new AuthoringBuildRetention();
            }

            public void Dispose()
            {
                if (m_IsDisposed)
                    return;

                m_IsDisposed = true;
                s_RetainedAssets = Array.Empty<UnityEngine.Object>();
                s_IsAcquired = false;
            }
        }
    }

    public sealed class CharacterSimulationProgramAssetPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (importedAssets.Any(IsRelevant) || deletedAssets.Any(IsRelevant) || movedAssets.Any(IsRelevant) || movedFromAssetPaths.Any(IsRelevant))
                CharacterSimulationProgramBuildService.ScheduleBuildAllStale();
        }

        static bool IsRelevant(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal))
                return false;
            string extension = Path.GetExtension(path);
            if (string.Equals(extension, ".inputactions", StringComparison.OrdinalIgnoreCase))
                return true;
            if (!string.Equals(extension, ".asset", StringComparison.OrdinalIgnoreCase))
                return false;
            Type type = AssetDatabase.GetMainAssetTypeAtPath(path);
            return type != typeof(CharacterSimulationProgramAsset) &&
                   type != typeof(CharacterPresentationProjectionAsset);
        }
    }
}
