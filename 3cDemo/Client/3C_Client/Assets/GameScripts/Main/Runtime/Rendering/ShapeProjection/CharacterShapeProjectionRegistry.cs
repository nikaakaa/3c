using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonRendering.ShapeProjection
{
    public static class CharacterShapeProjectionRegistry
    {
        static readonly List<CharacterShapeProjectionSource> Sources = new List<CharacterShapeProjectionSource>(32);

        public static int Count => Sources.Count;

        public static CharacterShapeProjectionSource Get(int index)
        {
            return Sources[index];
        }

        public static bool TryRegister(CharacterShapeProjectionSource source, out string error)
        {
            if (source == null)
            {
                error = "不能登记空Shape Projection Source";
                return false;
            }

            for (int i = Sources.Count - 1; i >= 0; i--)
            {
                if (Sources[i] == null)
                    Sources.RemoveAt(i);
            }

            for (int i = 0; i < Sources.Count; i++)
            {
                CharacterShapeProjectionSource current = Sources[i];
                if (current == source)
                {
                    error = string.Empty;
                    return true;
                }
                if (current != null && current.SourceId.Equals(source.SourceId))
                {
                    error = $"Shape Projection SourceId重复：{source.SourceId}";
                    return false;
                }
            }

            int insertIndex = 0;
            while (insertIndex < Sources.Count
                   && string.Compare(Sources[insertIndex].SourceId.Value, source.SourceId.Value, StringComparison.Ordinal) < 0)
                insertIndex++;
            Sources.Insert(insertIndex, source);
            error = string.Empty;
            return true;
        }

        public static void Unregister(CharacterShapeProjectionSource source)
        {
            if (source != null)
                Sources.Remove(source);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            Sources.Clear();
        }
    }
}
