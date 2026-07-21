using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics
{
    public sealed class CharacterRuntimeDebugProgram : IRuntimeDebugProgram
    {
        public CharacterRuntimeDebugProgram(RuntimeProgramRevision revision, DebugSourceMap sourceMap)
        {
            Revision = revision;
            SourceMap = sourceMap;
        }

        public RuntimeProgramRevision Revision { get; }
        public IDebugSourceMap SourceMap { get; }
    }

    public static class CharacterRuntimeDebugProgramBuilder
    {
        public static CharacterRuntimeDebugProgram Build(CharacterSimulationProgram program)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));

            return Build(
                program.Manifest.ProgramId.Value,
                program.Manifest.SourceRevision.Value,
                program.ProgramHash.ToString(),
                program.SourceMap);
        }

        public static CharacterRuntimeDebugProgram Build(
            string programId,
            string sourceRevision,
            string programHash,
            IReadOnlyList<ProgramSourceMapEntry> entries)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            var revision = new RuntimeProgramRevision(
                new ProgramId(programId).Value,
                new ProgramRevision(sourceRevision).Value,
                new StableHash(programHash).Value);
            var sourceMap = new DebugSourceMap(revision);
            var containers = new Dictionary<RuntimeSourceElementKey, RuntimeSourceElementHandle>();
            Dictionary<RuntimeSourceElementKey, string> containerContentHashes = ResolveContainerContentHashes(entries);
            for (int i = 0; i < entries.Count; i++)
            {
                ProgramSourceMapEntry entry = entries[i];
                RuntimeSourceElementKey source = ResolveSource(entry);
                string contentHash = ResolveContentHash(source, entry.ContentHash, containerContentHashes, programHash);
                RuntimeSourceElementHandle parent = EnsureParent(sourceMap, containers, source, containerContentHashes, programHash);
                sourceMap.Add(
                    source,
                    parent,
                    string.IsNullOrEmpty(entry.DisplayPath) ? source.ToString() : entry.DisplayPath,
                    contentHash,
                    ResolveTarget(entry));
            }
            sourceMap.Seal();
            return new CharacterRuntimeDebugProgram(revision, sourceMap);
        }

        static RuntimeSourceElementHandle EnsureParent(
            DebugSourceMap map,
            Dictionary<RuntimeSourceElementKey, RuntimeSourceElementHandle> containers,
            RuntimeSourceElementKey source,
            IReadOnlyDictionary<RuntimeSourceElementKey, string> contentHashes,
            string programHash)
        {
            switch (source.Kind)
            {
                case RuntimeSourceElementKind.Node:
                case RuntimeSourceElementKind.Edge:
                case RuntimeSourceElementKind.BlackboardDeclaration:
                    return EnsureContainer(
                        map,
                        containers,
                        RuntimeSourceElementKey.Graph(source.GraphAuthoringId),
                        default,
                        source.GraphAuthoringId,
                        ResolveContainerContentHash(RuntimeSourceElementKey.Graph(source.GraphAuthoringId), contentHashes, programHash));
                case RuntimeSourceElementKind.Track:
                    return EnsureContainer(
                        map,
                        containers,
                        RuntimeSourceElementKey.Timeline(source.TimelineAuthoringId),
                        default,
                        source.TimelineAuthoringId,
                        ResolveContainerContentHash(RuntimeSourceElementKey.Timeline(source.TimelineAuthoringId), contentHashes, programHash));
                case RuntimeSourceElementKind.Clip:
                case RuntimeSourceElementKind.TreeClip:
                    RuntimeSourceElementHandle timeline = EnsureContainer(
                        map,
                        containers,
                        RuntimeSourceElementKey.Timeline(source.TimelineAuthoringId),
                        default,
                        source.TimelineAuthoringId,
                        ResolveContainerContentHash(RuntimeSourceElementKey.Timeline(source.TimelineAuthoringId), contentHashes, programHash));
                    return EnsureContainer(
                        map,
                        containers,
                        RuntimeSourceElementKey.Track(source.TimelineAuthoringId, source.TrackAuthoringId),
                        timeline,
                        source.TrackAuthoringId,
                        ResolveContainerContentHash(RuntimeSourceElementKey.Timeline(source.TimelineAuthoringId), contentHashes, programHash));
                default:
                    return default;
            }
        }

        static RuntimeSourceElementHandle EnsureContainer(
            DebugSourceMap map,
            Dictionary<RuntimeSourceElementKey, RuntimeSourceElementHandle> containers,
            RuntimeSourceElementKey source,
            RuntimeSourceElementHandle parent,
            string displayName,
            string contentHash)
        {
            if (containers.TryGetValue(source, out RuntimeSourceElementHandle handle))
                return handle;
            handle = map.Add(source, parent, displayName, contentHash, RuntimeSourceTarget.Source);
            containers.Add(source, handle);
            return handle;
        }

        static Dictionary<RuntimeSourceElementKey, string> ResolveContainerContentHashes(IReadOnlyList<ProgramSourceMapEntry> entries)
        {
            var hashes = new Dictionary<RuntimeSourceElementKey, string>();
            for (int i = 0; i < entries.Count; i++)
            {
                ProgramSourceMapEntry entry = entries[i];
                if (string.IsNullOrEmpty(entry.ContentHash))
                    continue;
                RuntimeSourceElementKey container = !string.IsNullOrEmpty(entry.TimelineId)
                    ? RuntimeSourceElementKey.Timeline(entry.TimelineId)
                    : !string.IsNullOrEmpty(entry.GraphId)
                        ? RuntimeSourceElementKey.Graph(entry.GraphId)
                        : default;
                if (!container.IsValid)
                    continue;
                if (hashes.TryGetValue(container, out string existing) && !string.Equals(existing, entry.ContentHash, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Program source '{container}' has conflicting content hashes.");
                hashes[container] = entry.ContentHash;
            }
            return hashes;
        }

        static string ResolveContentHash(
            RuntimeSourceElementKey source,
            string contentHash,
            IReadOnlyDictionary<RuntimeSourceElementKey, string> containerContentHashes,
            string programHash)
        {
            if (!string.IsNullOrEmpty(contentHash))
                return contentHash;
            RuntimeSourceElementKey container = source.Kind switch
            {
                RuntimeSourceElementKind.Node or RuntimeSourceElementKind.Edge or RuntimeSourceElementKind.BlackboardDeclaration => RuntimeSourceElementKey.Graph(source.GraphAuthoringId),
                RuntimeSourceElementKind.Timeline or RuntimeSourceElementKind.Track or RuntimeSourceElementKind.Clip or RuntimeSourceElementKind.TreeClip => RuntimeSourceElementKey.Timeline(source.TimelineAuthoringId),
                _ => default
            };
            return ResolveContainerContentHash(container, containerContentHashes, programHash);
        }

        static string ResolveContainerContentHash(
            RuntimeSourceElementKey source,
            IReadOnlyDictionary<RuntimeSourceElementKey, string> contentHashes,
            string programHash)
        {
            if (!source.IsValid)
                return programHash;
            if (contentHashes.TryGetValue(source, out string contentHash))
                return contentHash;
            if (source.Kind == RuntimeSourceElementKind.Graph &&
                string.Equals(source.GraphAuthoringId, "Program", StringComparison.Ordinal))
            {
                return programHash;
            }
            throw new InvalidOperationException($"Program authoring container '{source}' has no content hash.");
        }

        static RuntimeSourceElementKey ResolveSource(ProgramSourceMapEntry source)
        {
            if (source.TargetKind == ProgramSourceTargetKind.BodyMotion)
                return RuntimeSourceElementKey.BodyMotionProfile(source.DisplayPath);
            if (!string.IsNullOrEmpty(source.ClipId))
            {
                bool treeClip = string.Equals(source.SourceType, typeof(TreeClip).FullName, StringComparison.Ordinal);
                return RuntimeSourceElementKey.Clip(source.TimelineId, source.TrackId, source.ClipId, treeClip);
            }
            if (!string.IsNullOrEmpty(source.TrackId))
                return RuntimeSourceElementKey.Track(source.TimelineId, source.TrackId);
            if (!string.IsNullOrEmpty(source.TimelineId))
                return RuntimeSourceElementKey.Timeline(source.TimelineId);
            if (!string.IsNullOrEmpty(source.DeclarationId))
                return RuntimeSourceElementKey.Declaration(source.GraphId, source.DeclarationId);
            if (!string.IsNullOrEmpty(source.NodeId))
                return RuntimeSourceElementKey.Node(source.GraphId, source.NodeId);
            if (!string.IsNullOrEmpty(source.EdgeId))
                return RuntimeSourceElementKey.Edge(source.GraphId, source.EdgeId);
            if (!string.IsNullOrEmpty(source.GraphId))
                return RuntimeSourceElementKey.Graph(source.GraphId);
            throw new InvalidOperationException($"Program source '{source.TargetKind}:{source.TargetIndex}' has no source identity.");
        }

        static RuntimeSourceTarget ResolveTarget(ProgramSourceMapEntry source)
        {
            RuntimeSourceTargetKind kind = source.TargetKind switch
            {
                ProgramSourceTargetKind.Operation => RuntimeSourceTargetKind.Operation,
                ProgramSourceTargetKind.Constant => RuntimeSourceTargetKind.Constant,
                ProgramSourceTargetKind.StateSlot => RuntimeSourceTargetKind.StateSlot,
                ProgramSourceTargetKind.Reference => RuntimeSourceTargetKind.Reference,
                ProgramSourceTargetKind.Producer => RuntimeSourceTargetKind.Producer,
                ProgramSourceTargetKind.CatalogEntry => RuntimeSourceTargetKind.CatalogEntry,
                ProgramSourceTargetKind.BodyMotion => RuntimeSourceTargetKind.BodyMotion,
                _ => throw new ArgumentOutOfRangeException(nameof(source.TargetKind))
            };
            return new RuntimeSourceTarget(kind, source.TargetIndex);
        }
    }
}
