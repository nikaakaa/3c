using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics
{
    public static class CharacterRuntimeDebugTargetResolver
    {
        public static RuntimeSourceTarget ForStateSlot(ProgramStateSlot slot)
        {
            if (slot == null)
                throw new ArgumentNullException(nameof(slot));
            return new RuntimeSourceTarget(RuntimeSourceTargetKind.StateSlot, slot.Index);
        }
    }

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
            var invocations = new Dictionary<string, ProgramSourceMapEntry>(StringComparer.Ordinal);
            foreach (ProgramSourceMapEntry entry in entries)
                if (entry.TargetKind == ProgramSourceTargetKind.GraphInvocation)
                    invocations.Add(entry.GraphInvocationPath, entry);
            foreach (ProgramSourceMapEntry entry in invocations.Values)
            {
                RuntimeSourceElementKey caller = default;
                if (!string.IsNullOrEmpty(entry.ParentInvocationPath))
                {
                    string parentGraph = ResolveGraphAuthoringId(invocations[entry.ParentInvocationPath]);
                    caller = ResolveInvocationCallerKey(entry, parentGraph, entries);
                }
                sourceMap.AddGraphInvocation(new RuntimeGraphInvocation(entry.GraphInvocationPath, ResolveGraphAuthoringId(entry),
                    entry.ParentInvocationPath, caller, entry.InvocationCallerClipId));
            }
            for (int i = 0; i < entries.Count; i++)
            {
                ProgramSourceMapEntry entry = entries[i];
                RuntimeSourceElementKey source = default;
                try
                {
                    source = ResolveSourceKey(entry);
                    string contentHash = ResolveContentHash(source, entry.ContentHash, containerContentHashes, programHash);
                    RuntimeSourceElementHandle parent = EnsureParent(sourceMap, containers, source, containerContentHashes, programHash);
                    sourceMap.Add(
                        source,
                        parent,
                        string.IsNullOrEmpty(entry.DisplayPath) ? source.ToString() : entry.DisplayPath,
                        contentHash,
                        ResolveTarget(entry));
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException(
                        $"Program source map entry has no valid runtime container: target={entry.TargetKind}:{entry.TargetIndex}; " +
                        $"type='{entry.SourceType}'; graph='{entry.GraphId}'; node='{entry.NodeId}'; " +
                        $"timeline='{entry.TimelineId}'; track='{entry.TrackId}'; clip='{entry.ClipId}'; " +
                        $"display='{entry.DisplayPath}'; keyKind={source.Kind}; keyGraph='{source.GraphAuthoringId}'; " +
                        $"keyTimeline='{source.TimelineAuthoringId}'; keyTrack='{source.TrackAuthoringId}'; detail={exception.Message}",
                        exception);
                }
            }
            sourceMap.Seal();
            foreach (ProgramSourceMapEntry entry in invocations.Values)
            {
                if (string.IsNullOrEmpty(entry.ParentInvocationPath))
                    continue;
                ProgramSourceMapEntry parent = invocations[entry.ParentInvocationPath];
                string parentGraph = ResolveGraphAuthoringId(parent);
                RuntimeSourceElementKey caller = ResolveInvocationCallerKey(entry, parentGraph, entries);
                if (!sourceMap.TryGetHandle(caller, out _))
                    throw new InvalidOperationException(
                        $"Graph invocation '{entry.GraphInvocationPath}' caller is absent from the Debug Source Map: " +
                        $"kind={caller.Kind}, graph='{caller.GraphAuthoringId}', element='{caller.ElementAuthoringId}', " +
                        $"edge='{caller.ElementAuthoringId}', timeline='{caller.TimelineAuthoringId}'.");
                if (entry.InvocationCallerKind != ProgramInvocationCallerKind.TimelineClip)
                    continue;
                bool hasCallerClip = false;
                for (int sourceIndex = 0; sourceIndex < entries.Count; sourceIndex++)
                {
                    ProgramSourceMapEntry source = entries[sourceIndex];
                    if (string.Equals(source.GraphId, parentGraph, StringComparison.Ordinal) &&
                        string.Equals(source.ClipId, entry.InvocationCallerClipId, StringComparison.Ordinal) &&
                        string.Equals(source.SourceType, typeof(TreeClip).FullName, StringComparison.Ordinal))
                    {
                        hasCallerClip = true;
                        break;
                    }
                }
                if (!hasCallerClip)
                    throw new InvalidOperationException(
                        $"Graph invocation '{entry.GraphInvocationPath}' caller TreeClip '{entry.InvocationCallerClipId}' is absent from the Debug Source Map.");
            }
            return new CharacterRuntimeDebugProgram(revision, sourceMap);
        }

        static RuntimeSourceElementKey ResolveInvocationCallerKey(
            ProgramSourceMapEntry entry,
            string parentGraph,
            IReadOnlyList<ProgramSourceMapEntry> entries)
        {
            RuntimeSourceElementKey requested = entry.InvocationCallerKind == ProgramInvocationCallerKind.Edge
                ? RuntimeSourceElementKey.Edge(parentGraph, entry.InvocationCallerId)
                : RuntimeSourceElementKey.Node(parentGraph, entry.InvocationCallerId);
            if (entry.InvocationCallerKind == ProgramInvocationCallerKind.TimelineClip)
                return requested;
            ProgramSourceMapEntry caller = FindInvocationCallerSource(entry, parentGraph, entries);
            if (caller == null)
                return requested;
            RuntimeSourceElementKey resolved = ResolveSourceKey(caller);
            return resolved.IsValid ? resolved : requested;
        }

        static ProgramSourceMapEntry FindInvocationCallerSource(
            ProgramSourceMapEntry invocation,
            string parentGraph,
            IReadOnlyList<ProgramSourceMapEntry> entries)
        {
            ProgramSourceMapEntry fallback = null;
            for (int i = 0; i < entries.Count; i++)
            {
                ProgramSourceMapEntry candidate = entries[i];
                bool matches = invocation.InvocationCallerKind == ProgramInvocationCallerKind.Edge
                    ? string.Equals(candidate.EdgeId, invocation.InvocationCallerId, StringComparison.Ordinal) &&
                      string.IsNullOrEmpty(candidate.ClipId)
                    : string.Equals(candidate.NodeId, invocation.InvocationCallerId, StringComparison.Ordinal) &&
                      string.IsNullOrEmpty(candidate.PortId) &&
                      string.IsNullOrEmpty(candidate.EdgeId) &&
                      string.IsNullOrEmpty(candidate.DeclarationId) &&
                      string.IsNullOrEmpty(candidate.TimelineId) &&
                      string.IsNullOrEmpty(candidate.TrackId) &&
                      string.IsNullOrEmpty(candidate.ClipId);
                if (!matches)
                    continue;
                if (fallback == null)
                    fallback = candidate;
                if (string.Equals(candidate.GraphId, parentGraph, StringComparison.Ordinal))
                    return candidate;
            }
            return fallback;
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
                case RuntimeSourceElementKind.Port:
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
                        RuntimeSourceElementKey.Timeline(source.TimelineAuthoringId, source.GraphAuthoringId),
                        default,
                        source.TimelineAuthoringId,
                        ResolveContainerContentHash(RuntimeSourceElementKey.Timeline(source.TimelineAuthoringId, source.GraphAuthoringId), contentHashes, programHash));
                case RuntimeSourceElementKind.Clip:
                case RuntimeSourceElementKind.TreeClip:
                    RuntimeSourceElementHandle timeline = EnsureContainer(
                        map,
                        containers,
                        RuntimeSourceElementKey.Timeline(source.TimelineAuthoringId, source.GraphAuthoringId),
                        default,
                        source.TimelineAuthoringId,
                        ResolveContainerContentHash(RuntimeSourceElementKey.Timeline(source.TimelineAuthoringId, source.GraphAuthoringId), contentHashes, programHash));
                    return EnsureContainer(
                        map,
                        containers,
                        RuntimeSourceElementKey.Track(source.TimelineAuthoringId, source.TrackAuthoringId, source.GraphAuthoringId),
                        timeline,
                        source.TrackAuthoringId,
                        ResolveContainerContentHash(RuntimeSourceElementKey.Timeline(source.TimelineAuthoringId, source.GraphAuthoringId), contentHashes, programHash));
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
            var hashEntries = new Dictionary<RuntimeSourceElementKey, ProgramSourceMapEntry>();
            for (int i = 0; i < entries.Count; i++)
            {
                ProgramSourceMapEntry entry = entries[i];
                if (string.IsNullOrEmpty(entry.ContentHash))
                    continue;
                RuntimeSourceElementKey container = !string.IsNullOrEmpty(entry.TimelineId)
                    ? RuntimeSourceElementKey.Timeline(entry.TimelineId, entry.GraphId)
                    : !string.IsNullOrEmpty(entry.GraphId)
                        ? RuntimeSourceElementKey.Graph(entry.GraphId)
                        : default;
                if (!container.IsValid)
                    continue;
                if (hashes.TryGetValue(container, out string existing) && !string.Equals(existing, entry.ContentHash, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        $"Program source container conflicts: kind={container.Kind}, graph={container.GraphAuthoringId}, " +
                        $"timeline={container.TimelineAuthoringId}, first={existing} ({DescribeEntry(hashEntries[container])}), " +
                        $"next={entry.ContentHash} ({DescribeEntry(entry)}).");
                hashes[container] = entry.ContentHash;
                hashEntries[container] = entry;
            }
            return hashes;
        }

        static string DescribeEntry(ProgramSourceMapEntry entry)
        {
            return $"{entry.TargetKind}:{entry.TargetIndex};type={entry.SourceType};graph={entry.GraphId};node={entry.NodeId};" +
                $"timeline={entry.TimelineId};track={entry.TrackId};clip={entry.ClipId};invocation={entry.GraphInvocationPath};sourceInvocation={entry.SourceInvocationPath}";
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
                RuntimeSourceElementKind.Node or RuntimeSourceElementKind.Port or RuntimeSourceElementKind.Edge or RuntimeSourceElementKind.BlackboardDeclaration => RuntimeSourceElementKey.Graph(source.GraphAuthoringId),
                RuntimeSourceElementKind.Timeline or RuntimeSourceElementKind.Track or RuntimeSourceElementKind.Clip or RuntimeSourceElementKind.TreeClip => RuntimeSourceElementKey.Timeline(source.TimelineAuthoringId, source.GraphAuthoringId),
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
            throw new InvalidOperationException(
                $"Program authoring container has no content hash: kind={source.Kind}, graph='{source.GraphAuthoringId}', " +
                $"element='{source.ElementAuthoringId}', timeline='{source.TimelineAuthoringId}', " +
                $"track='{source.TrackAuthoringId}', clip='{source.ClipAuthoringId}', port='{source.PortAuthoringId}'.");
        }

        public static RuntimeSourceElementKey ResolveSourceKey(ProgramSourceMapEntry source)
        {
            string graphId = ResolveGraphAuthoringId(source);
            if (source.TargetKind == ProgramSourceTargetKind.GraphInvocation)
                return RuntimeSourceElementKey.Graph(graphId);
            if (source.TargetKind == ProgramSourceTargetKind.OperationPort)
                return RuntimeSourceElementKey.Port(graphId, source.NodeId, source.PortId);
            if (source.TargetKind == ProgramSourceTargetKind.BodyMotion)
                return RuntimeSourceElementKey.BodyMotionProfile(source.DisplayPath);
            if (!string.IsNullOrEmpty(source.ClipId))
            {
                bool treeClip = string.Equals(source.SourceType, typeof(TreeClip).FullName, StringComparison.Ordinal);
                return RuntimeSourceElementKey.Clip(source.TimelineId, source.TrackId, source.ClipId, treeClip, graphId);
            }
            if (!string.IsNullOrEmpty(source.TrackId))
                return RuntimeSourceElementKey.Track(source.TimelineId, source.TrackId, graphId);
            if (!string.IsNullOrEmpty(source.TimelineId))
                return RuntimeSourceElementKey.Timeline(source.TimelineId, graphId);
            if (!string.IsNullOrEmpty(source.DeclarationId))
                return RuntimeSourceElementKey.Declaration(graphId, source.DeclarationId);
            if (!string.IsNullOrEmpty(source.NodeId))
                return RuntimeSourceElementKey.Node(graphId, source.NodeId);
            if (!string.IsNullOrEmpty(source.EdgeId))
                return RuntimeSourceElementKey.Edge(graphId, source.EdgeId);
            if (!string.IsNullOrEmpty(source.GraphId))
                return RuntimeSourceElementKey.Graph(graphId);
            throw new InvalidOperationException($"Program source '{source.TargetKind}:{source.TargetIndex}' has no source identity.");
        }

        public static string ResolveGraphAuthoringId(ProgramSourceMapEntry source)
        {
            if (source == null || string.IsNullOrEmpty(source.GraphId))
                return string.Empty;
            return source.GraphId;
        }

        static RuntimeSourceTarget ResolveTarget(ProgramSourceMapEntry source)
        {
            if (source.TargetKind is ProgramSourceTargetKind.OperationPort or
                ProgramSourceTargetKind.GraphInvocation or
                ProgramSourceTargetKind.OptimizedAway)
                return RuntimeSourceTarget.Source;
            RuntimeSourceTargetKind kind = source.TargetKind switch
            {
                ProgramSourceTargetKind.Operation => RuntimeSourceTargetKind.Operation,
                ProgramSourceTargetKind.Constant => RuntimeSourceTargetKind.Constant,
                ProgramSourceTargetKind.StateSlot => RuntimeSourceTargetKind.StateSlot,
                ProgramSourceTargetKind.Reference => RuntimeSourceTargetKind.Reference,
                ProgramSourceTargetKind.Producer => RuntimeSourceTargetKind.Producer,
                ProgramSourceTargetKind.CatalogEntry => RuntimeSourceTargetKind.CatalogEntry,
                ProgramSourceTargetKind.BodyMotion => RuntimeSourceTargetKind.BodyMotion,
                ProgramSourceTargetKind.ControlModule => RuntimeSourceTargetKind.ControlModule,
                ProgramSourceTargetKind.ControlTransition => RuntimeSourceTargetKind.ControlTransition,
                _ => throw new ArgumentOutOfRangeException(nameof(source.TargetKind))
            };
            return new RuntimeSourceTarget(kind, source.TargetIndex);
        }
    }
}
