using System;
using System.Collections.Generic;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    public abstract class SimulationPipelinePassDefinition : ScriptableObject
    {
        [SerializeField] string m_PassId = string.Empty;
        [SerializeField] string m_ImplementationVersion = string.Empty;

        public string PassId => m_PassId;
        public string ImplementationVersion => m_ImplementationVersion;
        public abstract SimulationPipelinePhase Phase { get; }

        public SimulationPipelinePassDescriptor BuildPortableDescriptor()
        {
            if (string.IsNullOrWhiteSpace(m_PassId) || string.IsNullOrWhiteSpace(m_ImplementationVersion))
                throw new InvalidOperationException($"Pipeline Pass Definition '{name}' requires explicit PassId and implementation version.");
            var passId = new SimulationPipelinePassId(m_PassId);
            var implementationVersion = new SimulationPipelinePassImplementationVersion(m_ImplementationVersion);
            SimulationPipelinePassDescriptor descriptor = BuildPortableDescriptor(passId, implementationVersion) ??
                throw new InvalidOperationException($"Pipeline Pass Definition '{m_PassId}' returned no portable descriptor.");
            if (!descriptor.PassId.Equals(passId) || !descriptor.ImplementationVersion.Equals(implementationVersion) || descriptor.Phase != Phase)
                throw new InvalidOperationException($"Pipeline Pass Definition '{m_PassId}' descriptor identity does not match its explicit authoring identity.");
            return descriptor;
        }

        protected abstract SimulationPipelinePassDescriptor BuildPortableDescriptor(
            SimulationPipelinePassId passId,
            SimulationPipelinePassImplementationVersion implementationVersion);

#if UNITY_EDITOR
        public void SetAuthoringIdentity(string passId, string implementationVersion)
        {
            m_PassId = SimulationIdentityAuthoring.Require(passId, nameof(passId));
            m_ImplementationVersion = SimulationIdentityAuthoring.Require(implementationVersion, nameof(implementationVersion));
        }
#endif
    }

    [CreateAssetMenu(fileName = "SimulationPipeline", menuName = "3C/Simulation/Pipeline Definition")]
    public class SimulationPipelineDefinition : ScriptableObject
    {
        [SerializeField] string m_PipelineId = string.Empty;
        [SerializeField] string m_Revision = string.Empty;
        [SerializeField, Min(1)] int m_SchemaVersion = 1;
        [SerializeField] SimulationPipelinePassDefinition[] m_IngressPasses = Array.Empty<SimulationPipelinePassDefinition>();
        [SerializeField] SimulationPipelinePassDefinition[] m_SchedulePasses = Array.Empty<SimulationPipelinePassDefinition>();
        [SerializeField] SimulationPipelinePassDefinition[] m_StepPasses = Array.Empty<SimulationPipelinePassDefinition>();
        [SerializeField] SimulationPipelinePassDefinition[] m_EgressPasses = Array.Empty<SimulationPipelinePassDefinition>();

        public string PipelineId => m_PipelineId;
        public string Revision => m_Revision;
        public int SchemaVersion => m_SchemaVersion;
        public IReadOnlyList<SimulationPipelinePassDefinition> IngressPasses => m_IngressPasses ?? Array.Empty<SimulationPipelinePassDefinition>();
        public IReadOnlyList<SimulationPipelinePassDefinition> SchedulePasses => m_SchedulePasses ?? Array.Empty<SimulationPipelinePassDefinition>();
        public IReadOnlyList<SimulationPipelinePassDefinition> StepPasses => m_StepPasses ?? Array.Empty<SimulationPipelinePassDefinition>();
        public IReadOnlyList<SimulationPipelinePassDefinition> EgressPasses => m_EgressPasses ?? Array.Empty<SimulationPipelinePassDefinition>();

        public virtual SimulationPipelineDescriptor BuildPortableDescriptor()
        {
            if (string.IsNullOrWhiteSpace(m_PipelineId) || string.IsNullOrWhiteSpace(m_Revision) || m_SchemaVersion <= 0)
                throw new InvalidOperationException($"Pipeline Definition '{name}' requires explicit PipelineId, revision and schema version.");
            return new SimulationPipelineDescriptor(
                new SimulationPipelineId(m_PipelineId),
                new SimulationPipelineRevision(m_Revision),
                new SimulationPipelineSchemaVersion(m_SchemaVersion),
                BuildPhase(IngressPasses, SimulationPipelinePhase.Ingress),
                BuildPhase(SchedulePasses, SimulationPipelinePhase.Schedule),
                BuildPhase(StepPasses, SimulationPipelinePhase.Step),
                BuildPhase(EgressPasses, SimulationPipelinePhase.Egress));
        }

        static IReadOnlyList<SimulationPipelinePassDescriptor> BuildPhase(
            IReadOnlyList<SimulationPipelinePassDefinition> definitions,
            SimulationPipelinePhase phase)
        {
            var descriptors = new List<SimulationPipelinePassDescriptor>(definitions.Count);
            for (int i = 0; i < definitions.Count; i++)
            {
                SimulationPipelinePassDefinition definition = definitions[i];
                if (!definition)
                    throw new InvalidOperationException($"Pipeline phase '{phase}' contains a missing Pass reference at index {i}.");
                if (definition.Phase != phase)
                    throw new InvalidOperationException($"Pipeline Pass '{definition.PassId}' belongs to '{definition.Phase}', not '{phase}'.");
                descriptors.Add(definition.BuildPortableDescriptor());
            }
            return descriptors.AsReadOnly();
        }

#if UNITY_EDITOR
        public void SetAuthoring(
            string pipelineId,
            string revision,
            int schemaVersion,
            SimulationPipelinePassDefinition[] ingress,
            SimulationPipelinePassDefinition[] schedule,
            SimulationPipelinePassDefinition[] step,
            SimulationPipelinePassDefinition[] egress)
        {
            if (schemaVersion <= 0)
                throw new ArgumentOutOfRangeException(nameof(schemaVersion));
            m_PipelineId = SimulationIdentityAuthoring.Require(pipelineId, nameof(pipelineId));
            m_Revision = SimulationIdentityAuthoring.Require(revision, nameof(revision));
            m_SchemaVersion = schemaVersion;
            m_IngressPasses = Copy(ingress);
            m_SchedulePasses = Copy(schedule);
            m_StepPasses = Copy(step);
            m_EgressPasses = Copy(egress);
        }

        static SimulationPipelinePassDefinition[] Copy(SimulationPipelinePassDefinition[] values)
        {
            return values == null ? Array.Empty<SimulationPipelinePassDefinition>() : (SimulationPipelinePassDefinition[])values.Clone();
        }
#endif
    }

    public static class SimulationIdentityAuthoring
    {
        public static string Require(string value, string parameter)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Explicit authoring identity is required.", parameter);
            return value.Trim();
        }
    }
}
