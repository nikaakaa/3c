using System;
using System.Collections.Generic;
using System.IO;
using BTSMTL.Timeline;
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

    public sealed class TimelineSimulationBuildRequest
    {
        readonly ICharacterSimulationTargetBuildAdapter[] m_Targets;

        public TimelineSimulationBuildRequest(
            TimelineAsset timeline,
            CharacterSimulationBuildPublicationMode publicationMode,
            IReadOnlyList<ICharacterSimulationTargetBuildAdapter> targets)
        {
            Timeline = timeline ? timeline : throw new ArgumentNullException(nameof(timeline));
            if (publicationMode != CharacterSimulationBuildPublicationMode.Publish &&
                publicationMode != CharacterSimulationBuildPublicationMode.DryRun)
            {
                throw new ArgumentOutOfRangeException(nameof(publicationMode));
            }
            PublicationMode = publicationMode;
            if (targets == null || targets.Count == 0)
                throw new ArgumentException("Timeline Simulation build requires at least one ordered Target Adapter.", nameof(targets));
            m_Targets = new ICharacterSimulationTargetBuildAdapter[targets.Count];
            var identities = new HashSet<NumericProfileId>();
            for (int i = 0; i < targets.Count; i++)
            {
                ICharacterSimulationTargetBuildAdapter target = targets[i] ??
                    throw new ArgumentException($"Timeline Simulation Target Adapter #{i} is null.", nameof(targets));
                if (!target.NumericProfileId.IsValid || !identities.Add(target.NumericProfileId))
                    throw new ArgumentException($"Timeline Simulation Target Adapter #{i} has an invalid or duplicate Numeric Profile.", nameof(targets));
                m_Targets[i] = target;
            }
        }

        public TimelineAsset Timeline { get; }
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
        ICharacterSimulationTargetPublishStage Stage(
            string definitionGuid,
            CharacterSimulationTargetBuildProduct product,
            bool publishWrapper);
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
        public Float32CharacterSimulationTargetBuildAdapter(
            string unityWrapperDestination,
            SimulationProgramRootKind rootKind = SimulationProgramRootKind.Character)
        {
            UnityWrapperDestination = RequireAssetPath(unityWrapperDestination, nameof(unityWrapperDestination));
            if (rootKind != SimulationProgramRootKind.Character && rootKind != SimulationProgramRootKind.Timeline)
                throw new ArgumentOutOfRangeException(nameof(rootKind));
            RootKind = rootKind;
        }

        public NumericProfileId NumericProfileId => Float32SimulationNumericProfile.Value.Id;
        public string UnityWrapperDestination { get; }
        public SimulationProgramRootKind RootKind { get; }

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
                        Float32SimulationNumericProfile.Value,
                        artifact.Header.Root));
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
            return Stage(definitionGuid, product, true);
        }

        public ICharacterSimulationTargetPublishStage Stage(
            string definitionGuid,
            CharacterSimulationTargetBuildProduct product,
            bool publishWrapper)
        {
            if (product is not Float32CharacterSimulationTargetBuildProduct typed)
                throw new ArgumentException("Float32 Target Adapter requires a Float32 build product.", nameof(product));
            return new Float32TargetPublishStage(
                CharacterTargetProgramArtifactStore.Stage(definitionGuid, typed.Program),
                typed,
                UnityWrapperDestination,
                RootKind,
                publishWrapper);
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
        public FixedCharacterSimulationTargetBuildAdapter(
            string unityWrapperDestination,
            SimulationProgramRootKind rootKind = SimulationProgramRootKind.Character)
        {
            if (string.IsNullOrWhiteSpace(unityWrapperDestination) ||
                !unityWrapperDestination.StartsWith("Assets/", StringComparison.Ordinal) ||
                !unityWrapperDestination.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Fixed Target Unity wrapper destination must be an Assets .asset path.", nameof(unityWrapperDestination));
            UnityWrapperDestination = unityWrapperDestination;
            if (rootKind != SimulationProgramRootKind.Character && rootKind != SimulationProgramRootKind.Timeline)
                throw new ArgumentOutOfRangeException(nameof(rootKind));
            RootKind = rootKind;
        }

        public NumericProfileId NumericProfileId => FixedSimulationNumericProfile.Value.Id;
        public string UnityWrapperDestination { get; }
        public SimulationProgramRootKind RootKind { get; }

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
            return Stage(definitionGuid, product, true);
        }

        public ICharacterSimulationTargetPublishStage Stage(
            string definitionGuid,
            CharacterSimulationTargetBuildProduct product,
            bool publishWrapper)
        {
            if (product is not FixedCharacterSimulationTargetBuildProduct typed)
                throw new ArgumentException("Fixed Target Adapter requires a Fixed build product.", nameof(product));
            return new FixedTargetPublishStage(definitionGuid, typed, UnityWrapperDestination, RootKind, publishWrapper);
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

        public static ICharacterSimulationTargetBuildAdapter Float32(TimelineAsset timeline)
        {
            if (!timeline)
                throw new ArgumentNullException(nameof(timeline));
            string timelinePath = AssetDatabase.GetAssetPath(timeline);
            string directory = Path.GetDirectoryName(timelinePath)?.Replace('\\', '/') ?? "Assets";
            return new Float32CharacterSimulationTargetBuildAdapter(
                $"{directory}/Generated/{timeline.name}.TimelineProgram.asset",
                SimulationProgramRootKind.Timeline);
        }

        public static ICharacterSimulationTargetBuildAdapter Fixed(TimelineAsset timeline)
        {
            if (!timeline)
                throw new ArgumentNullException(nameof(timeline));
            string timelinePath = AssetDatabase.GetAssetPath(timeline);
            string directory = Path.GetDirectoryName(timelinePath)?.Replace('\\', '/') ?? "Assets";
            return new FixedCharacterSimulationTargetBuildAdapter(
                $"{directory}/Generated/{timeline.name}.FixedTimelineProgram.asset",
                SimulationProgramRootKind.Timeline);
        }

        public static IReadOnlyList<ICharacterSimulationTargetBuildAdapter> DefaultEditor(TimelineAsset timeline)
        {
            return new ICharacterSimulationTargetBuildAdapter[]
            {
                Float32(timeline)
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
        readonly Type m_WrapperType;
        readonly bool m_PublishWrapper;
        ScriptableObject m_Wrapper;
        bool m_Committed;
        bool m_Completed;

        public Float32TargetPublishStage(
            CharacterTargetProgramArtifactPublishTransaction artifact,
            Float32CharacterSimulationTargetBuildProduct product,
            string path,
            SimulationProgramRootKind rootKind,
            bool publishWrapper)
        {
            m_Artifact = artifact ?? throw new ArgumentNullException(nameof(artifact));
            m_Product = product ?? throw new ArgumentNullException(nameof(product));
            m_Path = path;
            m_PublishWrapper = publishWrapper;
            m_WrapperType = WrapperType(rootKind);
            if (!publishWrapper)
                return;
            m_Wrapper = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (m_Wrapper && !m_WrapperType.IsInstanceOfType(m_Wrapper))
                throw new InvalidOperationException($"Float32 target wrapper at '{path}' has type '{m_Wrapper.GetType().FullName}', expected '{m_WrapperType.FullName}'.");
            m_Create = !m_Wrapper;
            m_Backup = m_Create ? string.Empty : EditorJsonUtility.ToJson(m_Wrapper);
            if (m_Create)
            {
                m_Wrapper = ScriptableObject.CreateInstance(m_WrapperType);
                m_Wrapper.name = Path.GetFileNameWithoutExtension(path);
            }
        }

        public UnityEngine.Object Wrapper => m_Wrapper;

        public void Commit()
        {
            Float32LoadedArtifact artifact = m_Artifact.Commit();
            m_Committed = true;
            if (!m_PublishWrapper)
                return;
            SetCompiledArtifact(m_Wrapper, artifact);
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
                if (!m_PublishWrapper)
                {
                    m_Artifact.Rollback();
                    m_Completed = true;
                    return;
                }
                if (m_Create && AssetDatabase.LoadAssetAtPath<ScriptableObject>(m_Path))
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

        static Type WrapperType(SimulationProgramRootKind rootKind)
        {
            return rootKind switch
            {
                SimulationProgramRootKind.Character => typeof(CharacterSimulationProgramAsset),
                SimulationProgramRootKind.Timeline => typeof(TimelineSimulationProgramAsset),
                _ => throw new ArgumentOutOfRangeException(nameof(rootKind))
            };
        }

        static void SetCompiledArtifact(ScriptableObject wrapper, Float32LoadedArtifact artifact)
        {
            switch (wrapper)
            {
                case CharacterSimulationProgramAsset character:
                    character.SetCompiledArtifact(artifact);
                    return;
                case TimelineSimulationProgramAsset timeline:
                    timeline.SetCompiledArtifact(artifact);
                    return;
                default:
                    throw new InvalidOperationException($"Unsupported Float32 target wrapper type '{wrapper?.GetType().FullName}'.");
            }
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
        readonly Type m_WrapperType;
        readonly bool m_PublishWrapper;
        ScriptableObject m_Wrapper;
        bool m_Committed;
        bool m_Completed;

        public FixedTargetPublishStage(
            string definitionGuid,
            FixedCharacterSimulationTargetBuildProduct product,
            string wrapperPath,
            SimulationProgramRootKind rootKind,
            bool publishWrapper)
        {
            ThirdPersonSimulation.Fixed.CharacterTargetProgramArtifactLoader.RequireDefinitionGuid(definitionGuid);
            m_DefinitionGuid = definitionGuid;
            m_Product = product ?? throw new ArgumentNullException(nameof(product));
            m_WrapperPath = wrapperPath;
            m_WrapperType = WrapperType(rootKind);
            m_PublishWrapper = publishWrapper;
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
                if (publishWrapper)
                {
                    m_Wrapper = AssetDatabase.LoadAssetAtPath<ScriptableObject>(wrapperPath);
                    if (m_Wrapper && !m_WrapperType.IsInstanceOfType(m_Wrapper))
                        throw new InvalidOperationException($"Fixed target wrapper at '{wrapperPath}' has type '{m_Wrapper.GetType().FullName}', expected '{m_WrapperType.FullName}'.");
                    m_CreateWrapper = !m_Wrapper;
                    m_WrapperBackup = m_CreateWrapper ? string.Empty : EditorJsonUtility.ToJson(m_Wrapper);
                    if (m_CreateWrapper)
                    {
                        m_Wrapper = ScriptableObject.CreateInstance(m_WrapperType);
                        m_Wrapper.name = Path.GetFileNameWithoutExtension(wrapperPath);
                    }
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
            if (!m_PublishWrapper)
                return;
            SetCompiledArtifact(m_Wrapper, published);
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
                if (!m_PublishWrapper)
                {
                    m_Completed = true;
                    return;
                }
                if (m_CreateWrapper && AssetDatabase.LoadAssetAtPath<ScriptableObject>(m_WrapperPath))
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

        static Type WrapperType(SimulationProgramRootKind rootKind)
        {
            return rootKind switch
            {
                SimulationProgramRootKind.Character => typeof(FixedCharacterSimulationProgramAsset),
                SimulationProgramRootKind.Timeline => typeof(FixedTimelineSimulationProgramAsset),
                _ => throw new ArgumentOutOfRangeException(nameof(rootKind))
            };
        }

        static void SetCompiledArtifact(
            ScriptableObject wrapper,
            FixedLoadedArtifact artifact)
        {
            switch (wrapper)
            {
                case FixedCharacterSimulationProgramAsset character:
                    character.SetCompiledArtifact(artifact);
                    return;
                case FixedTimelineSimulationProgramAsset timeline:
                    timeline.SetCompiledArtifact(artifact);
                    return;
                default:
                    throw new InvalidOperationException($"Unsupported Fixed target wrapper type '{wrapper?.GetType().FullName}'.");
            }
        }
    }
}
