using System;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonGameplay.Lab
{
    [CreateAssetMenu(fileName = "GameplayLabSessionVariant", menuName = "3C/Gameplay Lab/Session Variant")]
    public sealed class GameplayLabSessionVariantDefinition : ScriptableObject
    {
        [SerializeField] string m_VariantId = string.Empty;
        [SerializeField] GameObject m_RuntimeRootPrefab;
        [SerializeField] string m_NumericProfileId = string.Empty;
        [SerializeField, Min(1)] int m_TargetAbiVersion;
        [SerializeField] string m_SourceId = string.Empty;
        [SerializeField] string m_PipelineId = string.Empty;
        [SerializeField] string m_SolverId = string.Empty;
        [SerializeField] string m_DefinitionGuid = string.Empty;
        [SerializeField] SimulationSessionCompositionDefinition m_Composition;
        [SerializeField] ScriptableObject m_Program;
        [SerializeField] CharacterPresentationProjectionAsset m_PresentationProjection;
        [SerializeField] SimulationWorldSolverDefinition m_WorldSolver;
        [SerializeField] ScriptableObject m_CollisionWorld;
        [SerializeField] string m_ExternalLaunchArgumentPrefix = string.Empty;

        public string VariantId => Require(m_VariantId, nameof(m_VariantId));
        public GameObject RuntimeRootPrefab => m_RuntimeRootPrefab ? m_RuntimeRootPrefab :
            throw new InvalidOperationException($"Gameplay Lab Variant '{name}' requires a runtime root Prefab.");
        public string NumericProfileId => Require(m_NumericProfileId, nameof(m_NumericProfileId));
        public int TargetAbiVersion => m_TargetAbiVersion > 0
            ? m_TargetAbiVersion
            : throw new InvalidOperationException($"Gameplay Lab Variant '{name}' requires a positive Target ABI version.");
        public string SourceId => Require(m_SourceId, nameof(m_SourceId));
        public string PipelineId => Require(m_PipelineId, nameof(m_PipelineId));
        public string SolverId => Require(m_SolverId, nameof(m_SolverId));
        public string DefinitionGuid => Require(m_DefinitionGuid, nameof(m_DefinitionGuid));
        public SimulationSessionCompositionDefinition Composition => m_Composition ? m_Composition :
            throw new InvalidOperationException($"Gameplay Lab Variant '{name}' requires an exact Composition.");
        public ScriptableObject Program => m_Program ? m_Program :
            throw new InvalidOperationException($"Gameplay Lab Variant '{name}' requires an exact primary Character Program.");
        public CharacterPresentationProjectionAsset PresentationProjection => m_PresentationProjection
            ? m_PresentationProjection
            : throw new InvalidOperationException($"Gameplay Lab Variant '{name}' requires an exact Presentation Projection.");
        public SimulationWorldSolverDefinition WorldSolver => m_WorldSolver ? m_WorldSolver :
            throw new InvalidOperationException($"Gameplay Lab Variant '{name}' requires an exact KCC Definition.");
        public ScriptableObject CollisionWorld => m_CollisionWorld;
        public bool IsExternalLaunchVariant => !string.IsNullOrEmpty(m_ExternalLaunchArgumentPrefix);
        public string ExternalLaunchArgumentPrefix => m_ExternalLaunchArgumentPrefix;

        public void RequireComplete()
        {
            _ = Program;
            _ = PresentationProjection;
            _ = WorldSolver;
            ValidateComposition(Composition);
        }

        public bool MatchesExternalLaunch(string[] arguments)
        {
            if (!IsExternalLaunchVariant || arguments == null)
                return false;
            for (int i = 0; i < arguments.Length; i++)
            {
                string argument = arguments[i];
                if (!string.IsNullOrEmpty(argument) &&
                    argument.StartsWith(m_ExternalLaunchArgumentPrefix, StringComparison.Ordinal) &&
                    argument.Length > m_ExternalLaunchArgumentPrefix.Length)
                {
                    return true;
                }
            }
            return false;
        }

        public void ValidateComposition(SimulationSessionCompositionDefinition composition)
        {
            if (!composition)
                throw new ArgumentNullException(nameof(composition));
            if (composition != Composition)
                throw new InvalidOperationException($"Gameplay Lab Variant '{VariantId}' targets another Composition.");
            composition.RequireComplete();
            SimulationProgramRuntimeDescriptor program = composition.ProgramRuntime.BuildDescriptor();
            SimulationSessionSourceDescriptor source = composition.SessionSource.BuildAuthoringDescriptor().Source;
            SimulationPipelineDescriptor pipeline = composition.Pipeline.BuildPortableDescriptor();
            SimulationWorldSolverDefinitionDescriptor solver = composition.WorldSolver.BuildDescriptor(composition.TickRate);
            if (!string.Equals(program.NumericProfileId.Value, NumericProfileId, StringComparison.Ordinal) ||
                program.TargetAbiVersion.Value != TargetAbiVersion ||
                !string.Equals(source.Identity.ComponentId, SourceId, StringComparison.Ordinal) ||
                !string.Equals(pipeline.PipelineId.Value, PipelineId, StringComparison.Ordinal) ||
                !string.Equals(solver.Identity.ComponentId, SolverId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Gameplay Lab Variant '{VariantId}' does not match runtime root Composition '{composition.name}'.");
            }
            if (composition.WorldSolver != WorldSolver)
                throw new InvalidOperationException($"Gameplay Lab Variant '{VariantId}' KCC closure is split.");
            SimulationSessionHost[] hosts = RuntimeRootPrefab.GetComponentsInChildren<SimulationSessionHost>(true);
            if (hosts.Length != 1 || hosts[0].Composition != composition)
                throw new InvalidOperationException($"Gameplay Lab Variant '{VariantId}' runtime root targets another Composition.");
        }

#if UNITY_EDITOR
        public void SetAuthoring(
            string variantId,
            GameObject runtimeRootPrefab,
            string numericProfileId,
            int targetAbiVersion,
            string sourceId,
            string pipelineId,
            string solverId,
            string definitionGuid,
            SimulationSessionCompositionDefinition composition,
            ScriptableObject program,
            CharacterPresentationProjectionAsset presentationProjection,
            SimulationWorldSolverDefinition worldSolver,
            ScriptableObject collisionWorld,
            string externalLaunchArgumentPrefix)
        {
            m_VariantId = variantId;
            m_RuntimeRootPrefab = runtimeRootPrefab;
            m_NumericProfileId = numericProfileId;
            m_TargetAbiVersion = targetAbiVersion;
            m_SourceId = sourceId;
            m_PipelineId = pipelineId;
            m_SolverId = solverId;
            m_DefinitionGuid = Require(definitionGuid, nameof(definitionGuid));
            m_Composition = composition ? composition : throw new ArgumentNullException(nameof(composition));
            m_Program = program ? program : throw new ArgumentNullException(nameof(program));
            m_PresentationProjection = presentationProjection
                ? presentationProjection
                : throw new ArgumentNullException(nameof(presentationProjection));
            m_WorldSolver = worldSolver ? worldSolver : throw new ArgumentNullException(nameof(worldSolver));
            m_CollisionWorld = collisionWorld;
            m_ExternalLaunchArgumentPrefix = string.IsNullOrEmpty(externalLaunchArgumentPrefix)
                ? string.Empty
                : RequireExternalLaunchArgumentPrefix(externalLaunchArgumentPrefix);
            _ = VariantId;
            _ = RuntimeRootPrefab;
            _ = NumericProfileId;
            _ = TargetAbiVersion;
            _ = SourceId;
            _ = PipelineId;
            _ = SolverId;
            ValidateComposition(composition);
        }
#endif

        static string RequireExternalLaunchArgumentPrefix(string value)
        {
            string prefix = Require(value, nameof(m_ExternalLaunchArgumentPrefix));
            if (!prefix.StartsWith("--", StringComparison.Ordinal) || !prefix.EndsWith("=", StringComparison.Ordinal))
                throw new InvalidOperationException("Gameplay Lab external launch argument prefix must use '--name=' form.");
            return prefix;
        }

        static string Require(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException($"Gameplay Lab Variant requires explicit '{field}'.");
            return value;
        }
    }
}
