namespace ThirdPersonCharacter.Pipeline.Simulation
{
    public enum CharacterSurfaceRole : byte
    {
        Unknown = 0,
        SharedGround = 1,
        CharacterTraversal = 2,
        FootPlacementSurface = 3
    }

    public static class CharacterSurfaceLayerRoles
    {
        public const string GroundName = "Ground";
        public const string CharacterTraversalName = "CharacterTraversal";
        public const string FootPlacementSurfaceName = "FootPlacementSurface";

        public const int GroundLayer = 9;
        public const int CharacterTraversalLayer = 10;
        public const int FootPlacementSurfaceLayer = 12;

        public const int GroundMask = 1 << GroundLayer;
        public const int CharacterTraversalMask = 1 << CharacterTraversalLayer;
        public const int FootPlacementSurfaceMask = 1 << FootPlacementSurfaceLayer;
        public const int FootPlacementGroundMask = GroundMask | FootPlacementSurfaceMask;

        public static CharacterSurfaceRole ResolveRole(int layer)
        {
            if (layer == GroundLayer)
                return CharacterSurfaceRole.SharedGround;
            if (layer == CharacterTraversalLayer)
                return CharacterSurfaceRole.CharacterTraversal;
            if (layer == FootPlacementSurfaceLayer)
                return CharacterSurfaceRole.FootPlacementSurface;
            return CharacterSurfaceRole.Unknown;
        }
    }
}
