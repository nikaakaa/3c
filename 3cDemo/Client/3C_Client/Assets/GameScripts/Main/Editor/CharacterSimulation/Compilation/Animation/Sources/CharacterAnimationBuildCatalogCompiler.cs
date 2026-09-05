using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation.ACL;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation
{
    internal sealed class CharacterAnimationBuildCatalogEntry
    {
        internal CharacterAnimationBuildCatalogEntry(
            string stableIdentity,
            AnimationClip authoringClip,
            CharacterAnimationSamplingBackendKind backend,
            CharacterAnimationScalarCurvePage nativeScalarPage)
        {
            StableIdentity = stableIdentity ?? throw new ArgumentNullException(nameof(stableIdentity));
            AuthoringClip = authoringClip;
            Backend = backend;
            NativeScalarPage = nativeScalarPage;
        }

        internal string StableIdentity { get; }
        internal AnimationClip AuthoringClip { get; }
        internal CharacterAnimationSamplingBackendKind Backend { get; }
        internal CharacterAnimationScalarCurvePage NativeScalarPage { get; set; }
        internal int ResourceCatalogIndex { get; set; }
        internal int GroupIndex { get; set; }
        internal int GroupClipIndex { get; set; }
        internal CharacterAclAnimationGroupArtifact GroupArtifact { get; set; }
    }

    internal sealed class CharacterAnimationBuildCatalog
    {
        internal CharacterAnimationBuildCatalog(
            IReadOnlyList<CharacterAnimationBuildCatalogEntry> entries,
            IReadOnlyList<CharacterAclAnimationGroupArtifact> artifacts,
            IReadOnlyList<CharacterAnimationCompiledResourceDescriptor> resources,
            string contentHash)
        {
            Entries = entries ?? throw new ArgumentNullException(nameof(entries));
            AnimationArtifacts = artifacts ?? throw new ArgumentNullException(nameof(artifacts));
            AnimationResources = resources ?? throw new ArgumentNullException(nameof(resources));
            ContentHash = string.IsNullOrWhiteSpace(contentHash)
                ? throw new ArgumentException("Animation build catalog content hash is required.", nameof(contentHash))
                : contentHash;
        }

        internal IReadOnlyList<CharacterAnimationBuildCatalogEntry> Entries { get; }
        internal IReadOnlyList<CharacterAclAnimationGroupArtifact> AnimationArtifacts { get; }
        internal IReadOnlyList<CharacterAnimationCompiledResourceDescriptor> AnimationResources { get; }
        internal string ContentHash { get; }
    }

    internal sealed class CharacterAnimationBuildCatalogCompiler
    {
        readonly CharacterAnimationBuildInput m_Input;
        readonly Dictionary<string, CharacterAnimationBuildCatalogEntry> m_Entries =
            new Dictionary<string, CharacterAnimationBuildCatalogEntry>(StringComparer.Ordinal);
        readonly Dictionary<string, List<CharacterAnimationBuildCatalogEntry>> m_Groups =
            new Dictionary<string, List<CharacterAnimationBuildCatalogEntry>>(StringComparer.Ordinal);
        readonly Dictionary<string, int> m_GroupIndices =
            new Dictionary<string, int>(StringComparer.Ordinal);
        readonly Dictionary<string, int> m_AclResourceGroupIndices =
            new Dictionary<string, int>(StringComparer.Ordinal);
        readonly Dictionary<string, int> m_AclGroupClipIndices;
        readonly Dictionary<string, CharacterAnimationSamplingBackendKind> m_ClipBackends =
            new Dictionary<string, CharacterAnimationSamplingBackendKind>(StringComparer.Ordinal);
        internal CharacterAnimationBuildCatalogCompiler(
            CharacterAnimationBuildInput input)
        {
            m_Input = input ?? throw new ArgumentNullException(nameof(input));
            m_AclGroupClipIndices = BuildAclGroupClipIndices();
            if (m_AclGroupClipIndices.Count == 0)
                return;
            string groupIdentity = GroupIdentity(
                CharacterAnimationSamplingBackendKind.Acl);
            var group = new List<CharacterAnimationBuildCatalogEntry>(
                m_AclGroupClipIndices.Count);
            for (int i = 0; i < m_AclGroupClipIndices.Count; i++)
                group.Add(null);
            m_Groups.Add(
                groupIdentity,
                group);
            m_GroupIndices.Add(groupIdentity, m_GroupIndices.Count);
            m_AclResourceGroupIndices.Add(
                groupIdentity,
                m_AclResourceGroupIndices.Count);
        }

        Dictionary<string, int> BuildAclGroupClipIndices()
        {
            var stableIdentities = new List<string>();
            for (int i = 0; i < m_Input.Profile.SourceResourceBindings.Count; i++)
            {
                CharacterAnimationSourceResourceBinding binding =
                    m_Input.Profile.SourceResourceBindings[i];
                if (binding == null ||
                    binding.Backend != CharacterAnimationSamplingBackendKind.Acl)
                    continue;
                binding.RequireValid();
                CharacterAnimationClipContentIdentity identity =
                    CharacterAnimationClipRegisteredCurveCatalog.ResolveIdentity(
                        binding.AuthoringClip);
                stableIdentities.Add(StableIdentity(
                    CharacterAnimationSamplingBackendKind.Acl,
                    identity));
            }
            stableIdentities.Sort(StringComparer.Ordinal);
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < stableIdentities.Count; i++)
            {
                if (!result.TryAdd(stableIdentities[i], i))
                    throw new InvalidOperationException(
                        $"ACL animation source resource identity '{stableIdentities[i]}' is duplicated.");
            }
            return result;
        }

        internal CharacterAnimationScalarCurvePage BuildNativeScalarPage(
            AnimationClip clip,
            List<string> errors)
        {
            if (!clip)
            {
                errors?.Add("Native Clip scalar build input is incomplete.");
                return null;
            }
            CharacterAnimationClipContentIdentity identity =
                CharacterAnimationClipRegisteredCurveCatalog.ResolveIdentity(clip);
            string stableIdentity = StableIdentity(
                CharacterAnimationSamplingBackendKind.NativeClip,
                identity);
            if (m_Entries.TryGetValue(stableIdentity, out CharacterAnimationBuildCatalogEntry existing))
                return existing.NativeScalarPage;
            CharacterAnimationBuildCatalogEntry entry = RegisterCore(
                identity,
                clip,
                CharacterAnimationSamplingBackendKind.NativeClip,
                null);
            try
            {
                if (m_Input.Profile.AnimationPropertyBindings.Count == 0)
                    return null;
                if (!m_Input.HasSourceRig)
                    throw new InvalidOperationException(
                        "Native Clip scalar compilation requires the assembled source Rig binding.");
                CharacterAnimationAuthoringSource source =
                    CharacterAnimationAuthoringReader.Read(
                        new CharacterAnimationAuthoringReadRequest(
                            clip,
                            identity,
                            m_Input.SourceRig,
                            m_Input.ParameterLayout,
                            m_Input.Profile.AnimationPropertyBindings,
                            Array.Empty<CharacterAnimationParameterCurveSourceBinding>(),
                            $"animation-clip/{identity.AssetGuid}/{identity.LocalFileId}",
                            CharacterAnimationBuildChannel.AnimatedProperty));
                CharacterAnimationSampleGrid grid = CharacterAnimationSampleGrid.Create(
                    source.DurationSeconds,
                    m_Input.Compression.SampleRate,
                    source.Looping);
                CharacterAnimationSampleSet samples = CharacterAnimationSourceSampler.Sample(source, grid);
                CharacterAnimationSamplingQualityEvaluation quality =
                    CharacterAnimationSamplingQualityEvaluator.Evaluate(
                        source,
                        samples,
                        new CharacterAnimationSamplingQualitySettings(
                            m_Input.Compression.TransformPrecision,
                            m_Input.Compression.ScalePrecision,
                            m_Input.Compression.RotationPrecisionDegrees,
                            m_Input.Compression.ScalarPrecision));
                if (!quality.Report.publishable)
                {
                    errors?.AddRange(quality.Report.errors);
                    return null;
                }
                CharacterAnimationScalarCurvePage page = samples.NativeScalarPage;
                entry.NativeScalarPage = page;
                return page;
            }
            catch (Exception exception)
            {
                errors?.Add($"Native Clip '{clip.name}' scalar page compilation failed: {exception.Message}");
                return null;
            }
        }

        internal CharacterAnimationBuildCatalogEntry RegisterReference(
            AnimationClip clip,
            CharacterAnimationSamplingBackendKind backend,
            CharacterAnimationScalarCurvePage nativeScalarPage,
            string sourceIdentity)
        {
            if (!Enum.IsDefined(typeof(CharacterAnimationSamplingBackendKind), backend))
                throw new ArgumentOutOfRangeException(nameof(backend));
            if (!clip)
                throw new ArgumentException("Animation catalog references require an authoring Clip.", nameof(clip));
            CharacterAnimationClipContentIdentity identity =
                CharacterAnimationClipRegisteredCurveCatalog.ResolveIdentity(clip);
            string stableIdentity = StableIdentity(backend, identity);
            CharacterAnimationBuildCatalogEntry entry = RegisterCore(
                identity,
                clip,
                backend,
                nativeScalarPage);
            if (entry.AuthoringClip && clip && entry.AuthoringClip != clip)
                throw new InvalidOperationException(
                    $"Animation catalog identity '{stableIdentity}' resolves to multiple authoring Clips.");
            if (sourceIdentity != null && sourceIdentity.Length == 0)
                throw new ArgumentException("Animation catalog source identity is empty.", nameof(sourceIdentity));
            return entry;
        }

        internal CharacterAnimationBuildCatalog Complete(List<string> errors)
        {
            CharacterAnimationBuildCatalogEntry[] entries = m_Entries.Values
                .OrderBy(value => value.StableIdentity, StringComparer.Ordinal)
                .ToArray();
            KeyValuePair<string, int>[] aclDeclarations = m_AclGroupClipIndices
                .OrderBy(value => value.Value)
                .ToArray();
            for (int i = 0; i < aclDeclarations.Length; i++)
            {
                if (!m_Entries.ContainsKey(aclDeclarations[i].Key))
                {
                    errors?.Add(
                        $"ACL animation source resource '{aclDeclarations[i].Key}' is not referenced by the compiled animation graph.");
                }
            }
            var artifacts = new CharacterAclAnimationGroupArtifact[
                m_AclResourceGroupIndices.Count];
            var resources = new CharacterAnimationCompiledResourceDescriptor[
                m_AclResourceGroupIndices.Count];
            KeyValuePair<string, int>[] aclGroups = m_AclResourceGroupIndices
                .OrderBy(value => value.Value)
                .ToArray();
            for (int groupOrder = 0; groupOrder < aclGroups.Length; groupOrder++)
            {
                string groupIdentity = aclGroups[groupOrder].Key;
                int resourceIndex = aclGroups[groupOrder].Value;
                if (!m_Groups.TryGetValue(
                        groupIdentity,
                        out List<CharacterAnimationBuildCatalogEntry> group))
                {
                    errors?.Add(
                        $"ACL animation group '{groupIdentity}' is missing from the build catalog.");
                    continue;
                }
                int groupIndex = m_GroupIndices[groupIdentity];
                bool groupIsValid = group.Count == m_AclGroupClipIndices.Count;
                for (int i = 0; groupIsValid && i < group.Count; i++)
                {
                    CharacterAnimationBuildCatalogEntry entry = group[i];
                    if (entry == null ||
                        entry.Backend != CharacterAnimationSamplingBackendKind.Acl ||
                        entry.GroupIndex != groupIndex ||
                        entry.GroupClipIndex != i ||
                        !m_AclGroupClipIndices.TryGetValue(
                            entry.StableIdentity,
                            out int expectedGroupClipIndex) ||
                        expectedGroupClipIndex != i)
                    {
                        groupIsValid = false;
                    }
                }
                if (!groupIsValid)
                {
                    errors?.Add(
                        $"ACL animation group '{groupIndex}' entries are not contiguous or do not match its declarations.");
                    continue;
                }
                if (!m_Input.HasSourceRig)
                {
                    for (int i = 0; i < group.Count; i++)
                    {
                        errors?.Add(
                            $"ACL animation catalog entry '{group[i].StableIdentity}' requires the assembled source Rig binding.");
                    }
                    continue;
                }
                var requests = new List<CharacterAclAnimationBuildRequest>(group.Count);
                try
                {
                    for (int i = 0; i < group.Count; i++)
                    {
                        CharacterAnimationBuildCatalogEntry entry = group[i];
                        if (!entry.AuthoringClip)
                            throw new InvalidOperationException(
                                $"ACL animation catalog entry '{entry.StableIdentity}' has no authoring Clip for the formal build.");
                        CharacterAnimationClipContentIdentity identity =
                            CharacterAnimationClipRegisteredCurveCatalog.ResolveIdentity(entry.AuthoringClip);
                        requests.Add(new CharacterAclAnimationBuildRequest(
                            new CharacterAnimationAuthoringReadRequest(
                                entry.AuthoringClip,
                                identity,
                                m_Input.SourceRig,
                                m_Input.ParameterLayout,
                                m_Input.Profile.AnimationPropertyBindings,
                                Array.Empty<CharacterAnimationParameterCurveSourceBinding>(),
                                $"animation-clip/{identity.AssetGuid}/{identity.LocalFileId}",
                                CharacterAnimationBuildChannel.Transform |
                                CharacterAnimationBuildChannel.AnimatedProperty),
                            m_Input.Compression));
                    }
                    CharacterAclAnimationGroupArtifact groupArtifact =
                        CharacterAclAnimationResourceBuilder.BuildGroup(
                            requests,
                            groupIndex,
                            m_Input.OwnerAssetGuid,
                            m_Input.NativeArtifactIdentity);
                    for (int i = 0; i < group.Count; i++)
                        group[i].GroupArtifact = groupArtifact;
                    artifacts[resourceIndex] = groupArtifact;
                    resources[resourceIndex] = groupArtifact.CreateDescriptor(resourceIndex);
                }
                catch (Exception exception)
                {
                    errors?.Add(
                        $"ACL animation group '{groupIndex}' build failed: {exception.Message}");
                }
            }
            var hashValues = new List<string>(entries.Length * 4 + 2)
            {
                "character-animation-build-catalog/v1",
                m_Input.OwnerAssetGuid,
                m_Input.SourceRig.RigId,
                m_Input.SourceRig.RigRevision,
                m_Input.ParameterLayout.Hash
            };
            for (int i = 0; i < entries.Length; i++)
            {
                hashValues.Add(entries[i].StableIdentity);
                hashValues.Add(entries[i].Backend.ToString());
                hashValues.Add(entries[i].ResourceCatalogIndex.ToString());
                hashValues.Add(entries[i].GroupIndex.ToString());
                hashValues.Add(entries[i].GroupClipIndex.ToString());
                hashValues.Add(entries[i].GroupArtifact?.GroupContentHash ?? string.Empty);
            }
            return new CharacterAnimationBuildCatalog(
                entries,
                artifacts,
                resources,
                CharacterAclHash.ComputeStrings(hashValues));
        }

        CharacterAnimationBuildCatalogEntry RegisterCore(
            CharacterAnimationClipContentIdentity identity,
            AnimationClip clip,
            CharacterAnimationSamplingBackendKind backend,
            CharacterAnimationScalarCurvePage nativeScalarPage)
        {
            string stableIdentity = StableIdentity(backend, identity);
            int aclGroupClipIndex = -1;
            if (backend == CharacterAnimationSamplingBackendKind.Acl &&
                !m_AclGroupClipIndices.TryGetValue(
                    stableIdentity,
                    out aclGroupClipIndex))
            {
                throw new InvalidOperationException(
                    $"ACL animation catalog entry '{stableIdentity}' has no declared source resource binding.");
            }
            string clipAssetIdentity = ClipAssetIdentity(identity);
            if (m_ClipBackends.TryGetValue(
                    clipAssetIdentity,
                    out CharacterAnimationSamplingBackendKind existingBackend) &&
                existingBackend != backend)
            {
                throw new InvalidOperationException(
                    $"Animation catalog Clip identity '{clipAssetIdentity}' has conflicting source backends.");
            }
            m_ClipBackends[clipAssetIdentity] = backend;
            if (m_Entries.TryGetValue(stableIdentity, out CharacterAnimationBuildCatalogEntry existing))
            {
                if (existing.Backend != backend)
                    throw new InvalidOperationException(
                        $"Animation catalog identity '{stableIdentity}' has conflicting source declarations.");
                if (existing.NativeScalarPage == null && nativeScalarPage != null)
                    existing.NativeScalarPage = nativeScalarPage;
                return existing;
            }
            var entry = new CharacterAnimationBuildCatalogEntry(
                stableIdentity,
                clip,
                backend,
                nativeScalarPage);
            m_Entries.Add(stableIdentity, entry);
            string groupIdentity = GroupIdentity(backend);
            if (!m_Groups.TryGetValue(groupIdentity, out List<CharacterAnimationBuildCatalogEntry> group))
            {
                if (backend == CharacterAnimationSamplingBackendKind.Acl)
                    throw new InvalidOperationException(
                        $"ACL animation group '{groupIdentity}' was not prepared.");
                group = new List<CharacterAnimationBuildCatalogEntry>();
                m_Groups.Add(groupIdentity, group);
                m_GroupIndices.Add(groupIdentity, m_GroupIndices.Count);
            }
            entry.GroupIndex = m_GroupIndices[groupIdentity];
            if (backend == CharacterAnimationSamplingBackendKind.Acl)
            {
                entry.ResourceCatalogIndex =
                    m_AclResourceGroupIndices[groupIdentity];
                entry.GroupClipIndex = aclGroupClipIndex;
                if (group[aclGroupClipIndex] != null)
                    throw new InvalidOperationException(
                        $"ACL animation group Clip index '{aclGroupClipIndex}' is duplicated.");
                group[aclGroupClipIndex] = entry;
            }
            else
            {
                entry.ResourceCatalogIndex = -1;
                entry.GroupClipIndex = -1;
                group.Add(entry);
            }
            return entry;
        }

        string GroupIdentity(CharacterAnimationSamplingBackendKind backend) =>
            string.Concat(
                m_Input.SourceRig.RigId,
                ":",
                m_Input.SourceRig.RigRevision,
                ":",
                m_Input.ParameterLayout.Hash,
                ":",
                m_Input.Compression.Revision,
                ":",
                m_Input.Compression.CompilerOptions,
                ":",
                m_Input.Compression.SampleRate,
                ":",
                m_Input.Compression.TransformPrecision,
                ":",
                m_Input.Compression.ScalePrecision,
                ":",
                m_Input.Compression.RotationPrecisionDegrees,
                ":",
                m_Input.Compression.ScalarPrecision,
                ":",
                backend);

        static string ClipAssetIdentity(
            CharacterAnimationClipContentIdentity identity) =>
            string.Concat(
                identity.AssetGuid,
                ":",
                identity.LocalFileId);

        static string StableIdentity(
            CharacterAnimationSamplingBackendKind backend,
            CharacterAnimationClipContentIdentity identity) =>
            string.Concat(
                ((int)backend).ToString(),
                ":",
                identity.AssetGuid,
                ":",
                identity.LocalFileId);
    }
}
