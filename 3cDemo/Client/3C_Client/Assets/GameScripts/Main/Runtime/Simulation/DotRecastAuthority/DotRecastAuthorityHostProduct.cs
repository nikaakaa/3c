using System;
using ThirdPersonSimulation.DotRecast;
using ThirdPersonSimulation.ServerAuthoritative;

namespace ThirdPersonSimulation.DotRecastAuthority
{
    public static class DotRecastAuthorityHostProduct
    {
        public const string ServerProductId = "thirdperson.server-product.dotrecast-authority";
        public const string HostProductToken = "thirdperson.authority-product.dotrecast-scene.v1";
        public const string LaunchKind = "dotrecast-authority-scene";
        public const int ManifestSchemaVersion = 1;

        public static readonly HostProductId ProductId = new HostProductId(HostProductToken);
        public static readonly ServerAuthoritativeAuthorityHostProductDescriptor Descriptor =
            new ServerAuthoritativeAuthorityHostProductDescriptor(
                ProductId,
                ServerAuthoritativeAuthorityHostRouteKind.InProcessAuthorityScene,
                LaunchKind,
                ManifestSchemaVersion,
                DotRecastWorldSolver.DescriptorDefinition.ImplementationId,
                DotRecastWorldSolver.DescriptorDefinition.Version,
                DotRecastWorldSolver.DescriptorDefinition.Capabilities,
                DotRecastWorldSolver.DescriptorDefinition.Features);

        public static ServerAuthoritativeAuthorityHostIdentity CreateSceneHostIdentity(
            string hostId,
            ServerAuthoritativeRoomId roomId)
        {
            if (string.IsNullOrWhiteSpace(hostId) || !roomId.IsValid)
                throw new ArgumentException("DotRecast Authority Scene host identity is invalid.");
            return Descriptor.CreateHostIdentity(hostId, roomId);
        }
    }
}
