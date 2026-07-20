using System;
using ThirdPersonGameplay.Networking.ServerAuthoritative;
using ThirdPersonSimulation;
using ThirdPersonSimulation.ServerAuthoritative;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    [CreateAssetMenu(fileName = "ServerAuthoritativeAuthorityPipeline", menuName = "3C/Simulation/Server Authoritative Authority Pipeline")]
    public sealed class ServerAuthoritativeAuthorityPipelineDefinition :
        SimulationPipelineDefinition,
        IFloat32SimulationPipelineRuntimePackageProvider
    {
        [SerializeField] ServerAuthoritativeHybridModelDefinition m_Model;

        ServerAuthoritativeHybridModelDefinition Model => m_Model
            ? m_Model
            : throw new InvalidOperationException($"Authority Pipeline '{name}' requires its Model Definition.");

        public override SimulationPipelineDescriptor BuildPortableDescriptor()
        {
            ServerAuthoritativeAuthorityPipelineCatalogSet catalog = BuildPortableCatalog();
            RequireAuthoringIdentity(catalog.Descriptor);
            return catalog.Descriptor;
        }

        internal ServerAuthoritativeAuthorityPipelineCatalogSet BuildPortableCatalog() =>
            ServerAuthoritativeAuthorityPipelineCatalog.Create(Model.Policy, Model.ReplicationPolicy);

        public Float32SimulationPipelineRuntimePackage BuildRuntimePackage() =>
            BuildPortableCatalog().RuntimePackage;

        void RequireAuthoringIdentity(SimulationPipelineDescriptor descriptor)
        {
            if (!string.Equals(PipelineId, descriptor.PipelineId.Value, StringComparison.Ordinal) ||
                !string.Equals(Revision, descriptor.Revision.Value, StringComparison.Ordinal) ||
                SchemaVersion != descriptor.SchemaVersion.Value)
            {
                throw new InvalidOperationException($"Authority Pipeline '{name}' authoring identity is not canonical.");
            }
        }

#if UNITY_EDITOR
        public void SetModel(ServerAuthoritativeHybridModelDefinition model)
        {
            m_Model = model ? model : throw new ArgumentNullException(nameof(model));
        }
#endif
    }
}
