using System;
using ThirdPersonSimulation.ServerAuthoritative;

namespace ThirdPersonSimulation.UnityAuthority
{
    public static class UnityAuthorityHostProduct
    {
        public const string ServerProductId = "thirdperson.server-product.unity-authority";
        public const string HostProductToken = "thirdperson.authority-product.unity-worker.v1";
        public const string LaunchKind = "unity-authority-worker";
        public const int ManifestSchemaVersion = 1;
        public const string AuthoritySolverImplementation = "Unity.CharacterController.WorldSolver";
        public const string AuthoritySolverVersion = "2";

        public static readonly HostProductId ProductId = new HostProductId(HostProductToken);
        public static readonly WorldCapability AuthoritySolverCapabilities =
            WorldCapability.BodyMotion |
            WorldCapability.Grounding |
            WorldCapability.Collision |
            WorldCapability.Reconstructible |
            WorldCapability.AirborneVerticalMotion;
        public static readonly WorldFeature AuthoritySolverFeatures =
            WorldFeature.Ground |
            WorldFeature.Slope |
            WorldFeature.Step |
            WorldFeature.WallSlide;
        public static readonly ServerAuthoritativeAuthorityHostProductDescriptor Descriptor =
            new ServerAuthoritativeAuthorityHostProductDescriptor(
                ProductId,
                ServerAuthoritativeAuthorityHostRouteKind.ExternalAuthorityWorker,
                LaunchKind,
                ManifestSchemaVersion,
                new SolverImplementationId(AuthoritySolverImplementation),
                AuthoritySolverVersion,
                AuthoritySolverCapabilities,
                AuthoritySolverFeatures);

        public static ServerAuthoritativeAuthorityHostIdentity CreateWorkerHostIdentity(
            ServerAuthoritativeProcessIdentity process)
        {
            if (!process.IsAuthority || !process.WorkerId.IsValid)
                throw new ArgumentException("Unity Authority Worker process identity is invalid.", nameof(process));
            return Descriptor.CreateHostIdentity(process.WorkerId.Value, process.RoomId);
        }
    }
}
