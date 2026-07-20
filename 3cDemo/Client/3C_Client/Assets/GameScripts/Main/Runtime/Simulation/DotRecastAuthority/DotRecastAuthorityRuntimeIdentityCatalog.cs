using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation.DotRecastAuthority
{
    public static class DotRecastAuthorityRuntimeIdentityCatalog
    {
        public const string CommitterId = "thirdperson.simulation.committer.dotrecast-authority-scene";
        public const string DiagnosticsId = "thirdperson.simulation.diagnostics.dotrecast-authority-scene";

        public static SimulationComponentIdentity BuildCommitter(
            IEnumerable<SimulationOutputRouteDescriptor> routes)
        {
            var values = routes == null
                ? new List<SimulationOutputRouteDescriptor>()
                : new List<SimulationOutputRouteDescriptor>(routes);
            values.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            if (values.Count == 0)
                throw new ArgumentException("DotRecast Authority Committer requires output routes.", nameof(routes));
            var hashParts = new string[values.Count + 1];
            hashParts[0] = CommitterId;
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0 && values[i - 1].ActorId == values[i].ActorId)
                    throw new ArgumentException("DotRecast Authority Committer contains duplicate Actor routes.", nameof(routes));
                hashParts[i + 1] = $"{values[i].ActorId}:{values[i].ConfigurationHash}";
            }
            return new SimulationComponentIdentity(
                SimulationComponentRole.Committer,
                CommitterId,
                "1",
                StableHash.Compute(hashParts));
        }

        public static SimulationComponentIdentity BuildDiagnostics(HostProductId hostProductId)
        {
            if (!hostProductId.IsValid)
                throw new ArgumentException("DotRecast Authority diagnostics requires a Host Product identity.", nameof(hostProductId));
            return new SimulationComponentIdentity(
                SimulationComponentRole.Diagnostics,
                DiagnosticsId,
                "1",
                StableHash.Compute(DiagnosticsId, hostProductId.Value));
        }
    }
}
