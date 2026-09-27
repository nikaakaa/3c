using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public static class CharacterAnimationQuaternionCurveAuthoring
    {
        public static int NormalizeKeySigns(AnimationClip clip)
        {
            if (!clip)
                throw new ArgumentNullException(nameof(clip));
            var groups = new Dictionary<string, EditorCurveBinding[]>(StringComparer.Ordinal);
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (binding.type != typeof(Transform) || !binding.propertyName.StartsWith("m_LocalRotation.", StringComparison.Ordinal))
                    continue;
                if (!groups.TryGetValue(binding.path, out EditorCurveBinding[] group))
                    groups.Add(binding.path, group = new EditorCurveBinding[4]);
                group["xyzw".IndexOf(binding.propertyName[binding.propertyName.Length - 1])] = binding;
            }
            var changedBindings = new List<EditorCurveBinding>();
            var changedCurves = new List<AnimationCurve>();
            foreach (EditorCurveBinding[] group in groups.Values)
            {
                var curves = new AnimationCurve[4];
                var keys = new Keyframe[4][];
                for (int component = 0; component < 4; component++)
                {
                    curves[component] = AnimationUtility.GetEditorCurve(clip, group[component]);
                    if (curves[component] == null)
                        throw new InvalidOperationException($"Incomplete quaternion curve in '{clip.name}'.");
                    keys[component] = curves[component].keys;
                    if (keys[component].Length != keys[0].Length)
                        throw new InvalidOperationException($"Quaternion key counts differ in '{clip.name}' at '{group[0].path}'.");
                }
                bool changed = false;
                for (int keyIndex = 0; keyIndex < keys[0].Length; keyIndex++)
                {
                    for (int component = 1; component < 4; component++)
                        if (keys[component][keyIndex].time != keys[0][keyIndex].time)
                            throw new InvalidOperationException($"Quaternion key times differ in '{clip.name}' at '{group[0].path}'.");
                    if (keyIndex == 0 || Quaternion.Dot(Value(keys, keyIndex - 1), Value(keys, keyIndex)) >= 0f)
                        continue;
                    changed = true;
                    for (int component = 0; component < 4; component++)
                    {
                        Keyframe key = keys[component][keyIndex];
                        key.value = -key.value;
                        key.inTangent = -key.inTangent;
                        key.outTangent = -key.outTangent;
                        keys[component][keyIndex] = key;
                    }
                }
                if (!changed)
                    continue;
                for (int component = 0; component < 4; component++)
                {
                    curves[component].keys = keys[component];
                    changedBindings.Add(group[component]);
                    changedCurves.Add(curves[component]);
                }
            }
            if (changedBindings.Count == 0)
                return 0;
            Undo.RecordObject(clip, "Normalize quaternion key signs");
            AnimationUtility.SetEditorCurves(clip, changedBindings.ToArray(), changedCurves.ToArray());
            EditorUtility.SetDirty(clip);
            return changedBindings.Count / 4;
        }

        static Quaternion Value(Keyframe[][] keys, int index) =>
            new Quaternion(keys[0][index].value, keys[1][index].value, keys[2][index].value, keys[3][index].value);
    }
}
