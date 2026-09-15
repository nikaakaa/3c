using System;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class CharacterPresentationAssetObjectIdentity
    {
        public static string Require(UnityEngine.Object value)
        {
            if (!value ||
                !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    value,
                    out string guid,
                    out long localFileId) ||
                string.IsNullOrWhiteSpace(guid) ||
                localFileId == 0)
            {
                throw new InvalidOperationException(
                    $"Asset object '{value?.name ?? "Missing"}' has no stable GUID/local file id.");
            }
            return string.Concat(guid, ":", localFileId.ToString("D20"));
        }
    }
}
