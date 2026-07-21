using System;
using System.Collections.Generic;
using System.IO;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using UnityEditor;
using UnityEngine;
using FixedLoadedArtifact = ThirdPersonSimulation.Fixed.LoadedCharacterTargetProgramArtifact;
using FixedCompilation = ThirdPersonSimulation.Fixed.FixedProgramArtifactCompilationResult;
using FixedProgram = ThirdPersonSimulation.Fixed.CharacterSimulationProgram;
using Float32Program = ThirdPersonSimulation.CharacterSimulationProgram;
using Float32LoadedArtifact = ThirdPersonSimulation.LoadedCharacterTargetProgramArtifact;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public enum CharacterSimulationBuildPublicationMode
    {
        Publish,
        DryRun
    }

    public sealed class CharacterSimulationBuildRequest
    {
        readonly ICharacterSimulationTargetBuildAdapter[] m_Targets;

        public CharacterSimulationBuildRequest(
            CharacterPipelineDefinition definition,
            CharacterSimulationBuildPublicationMode publicationMode,
            IReadOnlyList<ICharacterSimulationTargetBuildAdapter> targets)
        {
            Definition = definition ? definition : throw new ArgumentNullException(nameof(definition));
            if (publicationMode != CharacterSimulationBuildPublicationMode.Publish &&
                publicationMode != CharacterSimulationBuildPublicationMode.DryRun)
            {
                throw new ArgumentOutOfRangeException(nameof(publicationMode));
            }
            PublicationMode = publicationMode;
            if (targets == null || targets.Count == 0)
                throw new ArgumentException("Character Simulation build requires at least one ordered Target Adapter.", nameof(targets));
            m_Targets = new ICharacterSimulationTargetBuildAdapter[targets.Count];
            var identities = new HashSet<NumericProfileId>();
            for (int i = 0; i < targets.Count; i++)
            {
                ICharacterSimulationTargetBuildAdapter target = targets[i] ??
                    throw new ArgumentException($"Character Simulation Target Adapter #{i} is null.", nameof(targets));
                if (!target.NumericProfileId.IsValid || !identities.Add(target.NumericProfileId))
                    throw new ArgumentException($"Character Simulation Target Adapter #{i} has an invalid or duplicate Numeric Profile.", nameof(targets));
                m_Targets[i] = target;
            }
        }

        public CharacterPipelineDefinition Definition { get; }
        public CharacterSimulationBuildPublicationMode PublicationMode { get; }
        public IReadOnlyList<ICharacterSimulationTargetBuildAdapter> Targets => m_Targets;
    }

    public abstract class CharacterSimulationTargetBuildProduct
    {
        protected CharacterSimulationTargetBuildProduct(CharacterPresentationSemanticContract contract)
        {
            Contract = contract ?? throw new ArgumentNullException(nameof(contract));
        }

        public CharacterPresentationSemanticContract Contract { get; }
        public abstract NumericProfileId NumericProfileId { get; }
    }

    public interface ICharacterSimulationTargetBuildAdapter
    {
        NumericProfileId NumericProfileId { get; }
        string UnityWrapperDestination { get; }
        CharacterSimulationTargetBuildProduct Compile(
            ValidatedSemanticIrArtifact artifact,
            CharacterSimulationCompileReport report);
        ICharacterSimulationTargetPublishStage Stage(
            string definitionGuid,
            CharacterSimulationTargetBuildProduct product);
    }

    public interface ICharacterSimulationTargetPublishStage : IDisposable
    {
        UnityEngine.Object Wrapper { get; }
        void Commit();
        void Complete();
        void Rollback();
    }

    public sealed class Float32CharacterSimulationTargetBuildProduct : CharacterSimulationTargetBuildProduct
    {
        public Float32CharacterSimulationTargetBuildProduct(
            Float32Program program,
            CharacterPresentationSemanticContract contract)
            : base(contract)
        {
            Program = program ?? throw new ArgumentNullException(nameof(program));
        }

        public Float32Program Program { get; }
        public override NumericProfileId NumericProfileId => Float32SimulationNumericProfile.Value.Id;
    }

    public sealed class FixedCharacterSimulationTargetBuildProduct : CharacterSimulationTargetBuildProduct
    {
        public FixedCharacterSimulationTargetBuildProduct(
            FixedCompilation compilation,
            CharacterPresentationSemanticContract contract)
            : base(contract)
        {
            Compilation = compilation ?? throw new ArgumentNullException(nameof(compilation));
        }

        public FixedCompilation Compilation { get; }
        public FixedProgram Program => Compilation.Program;
        public override NumericProfileId NumericProfileId => FixedSimulationNumericProfile.Value.Id;
    }

    public sealed class Float32CharacterSimulationTargetBuildAdapter : ICharacterSimulationTargetBuildAdapter
    {
        public Float32CharacterSimulationTargetBuildAdapter(string unityWrapperDestination)
        {
            UnityWrapperDestination = RequireAssetPath(unityWrapperDestination, nameof(unityWrapperDestination));
        }

        public NumericProfileId NumericProfileId => Float32SimulationNumericProfile.Value.Id;
        public string UnityWrapperDestination { get; }

        public CharacterSimulationTargetBuildProduct Compile(
            ValidatedSemanticIrArtifact artifact,
            CharacterSimulationCompileReport report)
        {
            try
            {
                Float32ProgramLoweringResult result = Float32CharacterSimulationTargetCompiler.Compile(artifact);
                for (int i = 0; i < result.Conversions.Count; i++)
                {
                    Float32ScalarConversion conversion = result.Conversions[i];
                    if (conversion.WasRounded)
                        report.TargetInformation("float32_literal_rounded", conversion.SourceIdentity, $"{conversion.SourceValue:R} -> {conversion.Value} error={conversion.AbsoluteError:R}.");
                }
                byte[] bytes = ThirdPersonSimulation.CharacterSimulationProgramCodec.WriteArtifact(result.Program);
                Float32Program roundTrip = ThirdPersonSimulation.CharacterSimulationProgramCodec.ReadArtifact(
                    bytes,
                    new ThirdPersonSimulation.ProgramLoadExpectation(
                        artifact.Header.CompilerVersion,
                        artifact.Header.OperationSetVersion,
                        artifact.Header.SourceRevision,
                        artifact.Header.SemanticHash,
                        Float32SimulationNumericProfile.Value));
                if (!roundTrip.ProgramHash.Equals(result.Program.ProgramHash) || !roundTrip.LayoutHash.Equals(result.Program.LayoutHash))
                    throw new InvalidDataException("Float32 Program round-trip identity mismatch.");
                var partitions = new HashSet<ProgramStateValueKind>();
                for (int i = 0; i < result.Program.StateSlots.Count; i++)
                    partitions.Add(result.Program.StateSlots[i].ValueKind);
                report.TargetInformation(
                    "float32_state_abi",
                    result.Program.Manifest.ProgramId.Value,
                    $"ABI={result.Program.Manifest.NumericProfile.AbiVersion.Value} Codec={ThirdPersonSimulation.CharacterSimulationStateCodec.CodecIdentity} StateSlots={result.Program.StateSlots.Count} TypedPartitions={partitions.Count} MotionTransientSlots=0 GameplayEffectAggregateSlots=1.");
                return new Float32CharacterSimulationTargetBuildProduct(
                    result.Program,
                    Float32CharacterPresentationContractAdapter.Create(result.Program));
            }
            catch (ThirdPersonSimulation.SimulationNumericConversionException exception)
            {
                report.TargetError("float32_numeric_conversion_failed", exception.SourceIdentity, exception.Message);
                return null;
            }
            catch (Exception exception)
            {
                report.TargetError("float32_target_failed", artifact.Header.ProgramId.Value, exception.Message);
                return null;
            }
        }

        public ICharacterSimulationTargetPublishStage Stage(
            string definitionGuid,
            CharacterSimulationTargetBuildProduct product)
        {
            if (product is not Float32CharacterSimulationTargetBuildProduct typed)
                throw new ArgumentException("Float32 Target Adapter requires a Float32 build product.", nameof(product));
            return new Float32TargetPublishStage(
                CharacterTargetProgramArtifactStore.Stage(definitionGuid, typed.Program),
                typed,
                UnityWrapperDestination);
        }

        static string RequireAssetPath(string value, string parameter)
        {
            if (string.IsNullOrWhiteSpace(value) || !value.StartsWith("Assets/", StringComparison.Ordinal) ||
                !value.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Target Unity wrapper destination must be an Assets .asset path.", parameter);
            return value;
        }
    }

    public sealed class FixedCharacterSimulationTargetBuildAdapter : ICharacterSimulationTargetBuildAdapter
    {
        public FixedCharacterSimulationTargetBuildAdapter(string unityWrapperDestination)
        {
            if (string.IsNullOrWhiteSpace(unityWrapperDestination) ||
                !unityWrapperDestination.StartsWith("Assets/", StringComparison.Ordinal) ||
                !unityWrapperDestination.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Fixed Target Unity wrapper destination must be an Assets .asset path.", nameof(unityWrapperDestination));
            UnityWrapperDestination = unityWrapperDestination;
        }

        public NumericProfileId NumericProfileId => FixedSimulationNumericProfile.Value.Id;
        public string UnityWrapperDestination { get; }

        public CharacterSimulationTargetBuildProduct Compile(
            ValidatedSemanticIrArtifact artifact,
            CharacterSimulationCompileReport report)
        {
            try
            {
                FixedCompilation compilation =
                    FixedCharacterSimulationTargetCompiler.CompileArtifact(artifact);
                return new FixedCharacterSimulationTargetBuildProduct(
                    compilation,
                    FixedCharacterPresentationContractAdapter.Create(compilation.Program));
            }
            catch (Exception exception)
            {
                report.TargetError("fixed_target_failed", artifact.Header.ProgramId.Value, exception.Message);
                return null;
            }
        }

        public ICharacterSimulationTargetPublishStage Stage(
            string definitionGuid,
            CharacterSimulationTargetBuildProduct product)
        {
            if (product is not FixedCharacterSimulationTargetBuildProduct typed)
                throw new ArgumentException("Fixed Target Adapter requires a Fixed build product.", nameof(product));
            return new FixedTargetPublishStage(definitionGuid, typed, UnityWrapperDestination);
        }
    }

    public static class CharacterSimulationTargetCatalog
    {
        public static ICharacterSimulationTargetBuildAdapter Float32(CharacterPipelineDefinition definition)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            string definitionPath = AssetDatabase.GetAssetPath(definition);
            string directory = Path.GetDirectoryName(definitionPath)?.Replace('\\', '/') ?? "Assets";
            return new Float32CharacterSimulationTargetBuildAdapter(
                $"{directory}/Generated/{definition.name}.SimulationProgram.asset");
        }

        public static IReadOnlyList<ICharacterSimulationTargetBuildAdapter> DefaultEditor(CharacterPipelineDefinition definition)
        {
            return new ICharacterSimulationTargetBuildAdapter[]
            {
                Float32(definition)
            };
        }
    }

    internal sealed class Float32TargetPublishStage : ICharacterSimulationTargetPublishStage
    {
        readonly CharacterTargetProgramArtifactPublishTransaction m_Artifact;
        readonly Float32CharacterSimulationTargetBuildProduct m_Product;
        readonly string m_Path;
        readonly bool m_Create;
        readonly string m_Backup;
        CharacterSimulationProgramAsset m_Wrapper;
        bool m_Committed;
        bool m_Completed;

        public Float32TargetPublishStage(
            CharacterTargetProgramArtifactPublishTransaction artifact,
            Float32CharacterSimulationTargetBuildProduct product,
            string path)
        {
            m_Artifact = artifact ?? throw new ArgumentNullException(nameof(artifact));
            m_Product = product ?? throw new ArgumentNullException(nameof(product));
            m_Path = path;
            m_Wrapper = AssetDatabase.LoadAssetAtPath<CharacterSimulationProgramAsset>(path);
            m_Create = !m_Wrapper;
            m_Backup = m_Create ? string.Empty : EditorJsonUtility.ToJson(m_Wrapper);
            if (m_Create)
            {
                m_Wrapper = ScriptableObject.CreateInstance<CharacterSimulationProgramAsset>();
                m_Wrapper.name = Path.GetFileNameWithoutExtension(path);
            }
        }

        public UnityEngine.Object Wrapper => m_Wrapper;

        public void Commit()
        {
            Float32LoadedArtifact artifact = m_Artifact.Commit();
            m_Committed = true;
            m_Wrapper.SetCompiledArtifact(artifact);
            EnsureAssetFolder(m_Path);
            if (m_Create)
                AssetDatabase.CreateAsset(m_Wrapper, m_Path);
            EditorUtility.SetDirty(m_Wrapper);
            AssetDatabase.SaveAssetIfDirty(m_Wrapper);
        }

        public void Complete()
        {
            m_Artifact.Complete();
            m_Completed = true;
        }

        public void Rollback()
        {
            if (m_Completed)
                return;
            if (m_Committed)
            {
                if (m_Create && AssetDatabase.LoadAssetAtPath<CharacterSimulationProgramAsset>(m_Path))
                    AssetDatabase.DeleteAsset(m_Path);
                else if (!m_Create)
                {
                    EditorJsonUtility.FromJsonOverwrite(m_Backup, m_Wrapper);
                    EditorUtility.SetDirty(m_Wrapper);
                    AssetDatabase.SaveAssetIfDirty(m_Wrapper);
                }
            }
            m_Artifact.Rollback();
            m_Completed = true;
        }

        public void Dispose()
        {
            if (!m_Completed)
                Rollback();
            m_Artifact.Dispose();
        }

        internal static void EnsureAssetFolder(string assetPath)
        {
            string folder = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            EnsureFolder(folder);
        }

        static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }

    internal sealed class FixedTargetPublishStage : ICharacterSimulationTargetPublishStage
    {
        readonly string m_Path;
        readonly string m_DefinitionGuid;
        readonly string m_TemporaryPath;
        readonly string m_BackupPath;
        readonly bool m_HadArtifact;
        readonly FixedCharacterSimulationTargetBuildProduct m_Product;
        readonly string m_WrapperPath;
        readonly bool m_CreateWrapper;
        readonly string m_WrapperBackup;
        FixedCharacterSimulationProgramAsset m_Wrapper;
        bool m_Committed;
        bool m_Completed;

        public FixedTargetPublishStage(
            string definitionGuid,
            FixedCharacterSimulationTargetBuildProduct product,
            string wrapperPath)
        {
            ThirdPersonSimulation.Fixed.CharacterTargetProgramArtifactLoader.RequireDefinitionGuid(definitionGuid);
            m_DefinitionGuid = definitionGuid;
            m_Product = product ?? throw new ArgumentNullException(nameof(product));
            m_WrapperPath = wrapperPath;
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            m_Path = Path.Combine(projectRoot, "Library", "CharacterSimulation", "Fixed", definitionGuid + ".fixed-program");
            Directory.CreateDirectory(Path.GetDirectoryName(m_Path) ?? throw new InvalidOperationException("Fixed Program artifact directory is unavailable."));
            string token = Guid.NewGuid().ToString("N");
            m_TemporaryPath = m_Path + "." + token + ".tmp";
            m_BackupPath = m_Path + "." + token + ".bak";
            m_HadArtifact = File.Exists(m_Path);
            try
            {
                File.WriteAllBytes(m_TemporaryPath, product.Compilation.CopyCanonicalBytes());
                ThirdPersonSimulation.Fixed.CharacterTargetProgramArtifactLoader.Inspect(definitionGuid, File.ReadAllBytes(m_TemporaryPath));
                m_Wrapper = AssetDatabase.LoadAssetAtPath<FixedCharacterSimulationProgramAsset>(wrapperPath);
                m_CreateWrapper = !m_Wrapper;
                m_WrapperBackup = m_CreateWrapper ? string.Empty : EditorJsonUtility.ToJson(m_Wrapper);
                if (m_CreateWrapper)
                {
                    m_Wrapper = ScriptableObject.CreateInstance<FixedCharacterSimulationProgramAsset>();
                    m_Wrapper.name = Path.GetFileNameWithoutExtension(wrapperPath);
                }
            }
            catch
            {
                if (File.Exists(m_TemporaryPath))
                    File.Delete(m_TemporaryPath);
                throw;
            }
        }

        public UnityEngine.Object Wrapper => m_Wrapper;

        public void Commit()
        {
            if (m_HadArtifact)
                File.Replace(m_TemporaryPath, m_Path, m_BackupPath);
            else
                File.Move(m_TemporaryPath, m_Path);
            m_Committed = true;
            FixedLoadedArtifact published = ThirdPersonSimulation.Fixed.CharacterTargetProgramArtifactLoader.Inspect(
                m_DefinitionGuid,
                File.ReadAllBytes(m_Path));
            m_Wrapper.SetCompiledArtifact(published);
            Float32TargetPublishStage.EnsureAssetFolder(m_WrapperPath);
            if (m_CreateWrapper)
                AssetDatabase.CreateAsset(m_Wrapper, m_WrapperPath);
            EditorUtility.SetDirty(m_Wrapper);
            AssetDatabase.SaveAssetIfDirty(m_Wrapper);
        }

        public void Complete()
        {
            m_Completed = true;
            try
            {
                if (File.Exists(m_BackupPath))
                    File.Delete(m_BackupPath);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        public void Rollback()
        {
            if (m_Completed)
                return;
            if (m_Committed)
            {
                if (m_HadArtifact)
                    File.Replace(m_BackupPath, m_Path, null);
                else if (File.Exists(m_Path))
                    File.Delete(m_Path);
                if (m_CreateWrapper && AssetDatabase.LoadAssetAtPath<FixedCharacterSimulationProgramAsset>(m_WrapperPath))
                    AssetDatabase.DeleteAsset(m_WrapperPath);
                else if (!m_CreateWrapper)
                {
                    EditorJsonUtility.FromJsonOverwrite(m_WrapperBackup, m_Wrapper);
                    EditorUtility.SetDirty(m_Wrapper);
                    AssetDatabase.SaveAssetIfDirty(m_Wrapper);
                }
            }
            if (File.Exists(m_TemporaryPath))
                File.Delete(m_TemporaryPath);
            if (File.Exists(m_BackupPath))
                File.Delete(m_BackupPath);
            m_Completed = true;
        }

        public void Dispose()
        {
            if (!m_Completed)
                Rollback();
        }
    }
}
