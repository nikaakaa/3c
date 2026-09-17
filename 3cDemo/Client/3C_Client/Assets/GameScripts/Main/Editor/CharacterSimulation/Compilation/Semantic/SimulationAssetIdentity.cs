using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public static class SimulationAssetIdentity
    {
        public static string Of(UnityEngine.Object asset)
        {
            if (!asset)
                return string.Empty;
            string path = AssetDatabase.GetAssetPath(asset);
            string guid = string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
            return string.IsNullOrEmpty(guid) ? string.Empty : $"asset:{guid}";
        }
    }
}
