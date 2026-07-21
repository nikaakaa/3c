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

        public void ValidateComposition(SimulationSessionCompositionDefinition composition)
        {
            if (!composition)
                throw new ArgumentNullException(nameof(composition));
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
        }

#if UNITY_EDITOR
        public void SetAuthoring(
            string variantId,
            GameObject runtimeRootPrefab,
            string numericProfileId,
            int targetAbiVersion,
            string sourceId,
            string pipelineId,
            string solverId)
        {
            m_VariantId = variantId;
            m_RuntimeRootPrefab = runtimeRootPrefab;
            m_NumericProfileId = numericProfileId;
            m_TargetAbiVersion = targetAbiVersion;
            m_SourceId = sourceId;
            m_PipelineId = pipelineId;
            m_SolverId = solverId;
            _ = VariantId;
            _ = RuntimeRootPrefab;
            _ = NumericProfileId;
            _ = TargetAbiVersion;
            _ = SourceId;
            _ = PipelineId;
            _ = SolverId;
        }
#endif

        static string Require(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException($"Gameplay Lab Variant requires explicit '{field}'.");
            return value;
        }
    }
}
