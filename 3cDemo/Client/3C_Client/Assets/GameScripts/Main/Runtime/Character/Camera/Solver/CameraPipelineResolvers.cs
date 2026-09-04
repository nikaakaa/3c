using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
    public sealed class CameraTargetBindingResolver
    {
        readonly Dictionary<string, Transform> m_Bindings =
            new Dictionary<string, Transform>(StringComparer.Ordinal);

        public CameraTargetBindingResolver(IReadOnlyList<CameraTargetBinding> bindings)
        {
            if (bindings == null)
                throw new ArgumentNullException(nameof(bindings));
            for (int i = 0; i < bindings.Count; i++)
            {
                CameraTargetBinding binding = bindings[i] ??
                    throw new ArgumentException($"Camera target binding #{i} is missing.", nameof(bindings));
                string key = ValidateKey(binding.Key, $"Camera target binding #{i}");
                if (!binding.Target)
                    throw new ArgumentException($"Camera target binding '{key}' has no Transform.", nameof(bindings));
                if (!m_Bindings.TryAdd(key, binding.Target))
                    throw new ArgumentException($"Camera target binding '{key}' is duplicated.", nameof(bindings));
            }
        }

        public void RequireKey(string key, string source)
        {
            if (string.IsNullOrEmpty(key))
                return;
            string required = ValidateKey(key, source);
            if (!m_Bindings.ContainsKey(required))
                throw new InvalidOperationException($"{source} requires Camera target binding '{required}'.");
        }

        public CameraResolvedTargetPlan Resolve(
            CameraSequenceRequest sequence,
            IEnumerable<CameraTargetSelectionRequest> requests)
        {
            CameraTargetSelectionRequest selected = default;
            if (requests != null)
            {
                foreach (CameraTargetSelectionRequest candidate in requests)
                {
                    if (candidate.Active && ShouldReplace(selected, candidate))
                        selected = candidate;
                }
            }

            if (!selected.Active && string.IsNullOrEmpty(sequence.TargetKey))
                return CameraResolvedTargetPlan.NoOverride;

            string sourceKey = selected.Active
                ? FirstKey(selected.AnchorKey, selected.AimPointKey, selected.PreferredBoneKey, selected.TargetKey)
                : sequence.TargetKey;
            string followKey = selected.Active ? selected.AnchorKey : string.Empty;
            string aimKey = selected.Active
                ? FirstKey(selected.AimPointKey, selected.PreferredBoneKey, selected.TargetKey)
                : sequence.TargetKey;
            bool hasFollow = !string.IsNullOrEmpty(followKey);
            bool hasAim = !string.IsNullOrEmpty(aimKey);
            Vector3 follow = default;
            Vector3 aim = default;
            if (hasFollow && !TryResolvePoint(followKey, out follow, out string followError))
                return CameraResolvedTargetPlan.Invalid(sourceKey, followError);
            if (hasAim && !TryResolvePoint(aimKey, out aim, out string aimError))
                return CameraResolvedTargetPlan.Invalid(sourceKey, aimError);
            return new CameraResolvedTargetPlan(true, hasFollow, follow, hasAim, aim, sourceKey, string.Empty);
        }

        public CameraTargetSnapshot CaptureSnapshot(string key)
        {
            if (string.IsNullOrEmpty(key) || !m_Bindings.TryGetValue(key, out Transform target) || !target)
                return default;
            return new CameraTargetSnapshot(
                key,
                target.position,
                target.position,
                Vector3.zero,
                true,
                true);
        }

        bool TryResolvePoint(string key, out Vector3 point, out string error)
        {
            point = default;
            error = string.Empty;
            if (!m_Bindings.TryGetValue(key, out Transform target))
            {
                error = $"Camera target binding '{key}' is absent.";
                return false;
            }
            if (!target)
            {
                error = $"Camera target binding '{key}' no longer references a live Transform.";
                return false;
            }
            point = target.position;
            return true;
        }

        static bool ShouldReplace(
            CameraTargetSelectionRequest selected,
            CameraTargetSelectionRequest candidate)
        {
            if (!selected.Active)
                return true;
            if (candidate.Priority != selected.Priority)
                return candidate.Priority > selected.Priority;
            if (!Mathf.Approximately(candidate.Weight, selected.Weight))
                return candidate.Weight > selected.Weight;
            return string.CompareOrdinal(candidate.SourceId, selected.SourceId) < 0;
        }

        static string FirstKey(params string[] keys)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (!string.IsNullOrEmpty(keys[i]))
                    return keys[i];
            }
            return string.Empty;
        }

        static string ValidateKey(string key, string source)
        {
            if (string.IsNullOrWhiteSpace(key) || !string.Equals(key, key.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException($"{source} contains an invalid Camera target key.");
            return key;
        }
    }

    public sealed class CameraSequenceRequestResolver
    {
        public CameraSequenceRequest Resolve(
            IReadOnlyList<CameraSequenceRequest> requests,
            string defaultSequenceId)
        {
            CameraSequenceRequest selected = default;
            if (requests != null)
            {
                for (int i = 0; i < requests.Count; i++)
                {
                    CameraSequenceRequest candidate = requests[i];
                    if (!candidate.Active || !ShouldReplace(selected, candidate))
                        continue;
                    selected = candidate;
                }
            }
            return selected.Active
                ? selected
                : new CameraSequenceRequest(
                    defaultSequenceId,
                    int.MinValue,
                    1f,
                    0f,
                    0f,
                    string.Empty,
                    "camera.default.sequence",
                    0,
                    0,
                    CameraSequenceInterruptPolicy.BlendOut,
                    true);
        }

        static bool ShouldReplace(CameraSequenceRequest selected, CameraSequenceRequest candidate)
        {
            if (!selected.Active)
                return true;
            if (candidate.Priority != selected.Priority)
                return candidate.Priority > selected.Priority;
            if (!Mathf.Approximately(candidate.Weight, selected.Weight))
                return candidate.Weight > selected.Weight;
            return string.CompareOrdinal(candidate.SourceId, selected.SourceId) < 0;
        }
    }

    public sealed class CameraResponseRequestResolver
    {
        readonly CameraInputSettings m_DefaultInput;

        public CameraResponseRequestResolver(CameraInputSettings defaultInput)
        {
            m_DefaultInput = defaultInput ?? throw new ArgumentNullException(nameof(defaultInput));
        }

        public CameraResponseRequest Resolve(IReadOnlyList<CameraResponseRequest> requests)
        {
            CameraResponseRequest selected = new CameraResponseRequest(
                CameraResponseMode.Weighted,
                m_DefaultInput.DefaultResponseWeight,
                m_DefaultInput.PitchResponseWeight,
                m_DefaultInput.YawResponseWeight,
                int.MinValue,
                1f,
                "camera.default.response",
                0,
                0);
            if (requests == null)
                return selected;
            for (int i = 0; i < requests.Count; i++)
            {
                CameraResponseRequest candidate = requests[i];
                if (!candidate.Active || candidate.Priority < selected.Priority ||
                    candidate.Priority == selected.Priority && candidate.Weight <= selected.Weight)
                    continue;
                selected = candidate;
            }
            return selected;
        }
    }
}
