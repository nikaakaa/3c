using System;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.MotionMatching
{
    public static class MotionMatchingSourceClipResolver
    {
        public static AnimationClip Resolve(CharacterMotionMatchingSourceClipEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));
            entry.RequireValid();
            string path = AssetDatabase.GUIDToAssetPath(entry.AnimationClipAssetGuid);
            if (string.IsNullOrEmpty(path))
                throw new InvalidOperationException($"SourceClipId '{entry.SourceClipId}' asset GUID cannot be resolved.");
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < assets.Length; i++)
            {
                if (!(assets[i] is AnimationClip clip))
                    continue;
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(clip, out string guid, out long localId) &&
                    string.Equals(guid, entry.AnimationClipAssetGuid, StringComparison.Ordinal) &&
                    localId == entry.AnimationClipLocalFileId)
                    return clip;
            }
            throw new InvalidOperationException($"SourceClipId '{entry.SourceClipId}' local file id no longer resolves to an AnimationClip.");
        }

        public static string DependencyHash(AnimationClip clip)
        {
            if (!clip)
                throw new ArgumentNullException(nameof(clip));
            string path = AssetDatabase.GetAssetPath(clip);
            if (string.IsNullOrEmpty(path))
                throw new InvalidOperationException("Motion Matching source Clip is not a persisted asset.");
            return AssetDatabase.GetAssetDependencyHash(path).ToString();
        }
    }
}
