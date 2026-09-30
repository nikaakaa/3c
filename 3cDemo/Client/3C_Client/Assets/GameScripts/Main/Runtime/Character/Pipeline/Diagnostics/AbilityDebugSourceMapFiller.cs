using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics
{
    public static class AbilityDebugSourceMapFiller
    {
        public static void Fill(DebugSourceMap sourceMap, int targetIndexOffset, IReadOnlyList<ProgramSourceMapEntry> sources)
        {
            var graphByPath = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (ProgramSourceMapEntry source in sources)
            {
                if (source.TargetKind != ProgramSourceTargetKind.GraphInvocation)
                    continue;
                graphByPath.TryAdd(source.SourceInvocationPath, source.GraphId);
                graphByPath.TryAdd(source.GraphInvocationPath, source.GraphId);
            }
            foreach (ProgramSourceMapEntry source in sources)
            {
                if (source.TargetKind == ProgramSourceTargetKind.OptimizedAway)
                    continue;
                RuntimeSourceElementKey key = SourceKey(source);
                var target = source.TargetKind == ProgramSourceTargetKind.OperationPort
                    || source.TargetKind == ProgramSourceTargetKind.GraphInvocation
                        ? RuntimeSourceTarget.Source
                        : new RuntimeSourceTarget(MapTargetKind(source.TargetKind), targetIndexOffset + source.TargetIndex);
                sourceMap.Add(key, default, source.DisplayPath, source.ContentHash, target);
                if (source.TargetKind == ProgramSourceTargetKind.GraphInvocation)
                {
                    sourceMap.AddGraphInvocation(new RuntimeGraphInvocation(
                        source.GraphInvocationPath, source.GraphId, source.ParentInvocationPath,
                        Caller(source, graphByPath), source.InvocationCallerClipId, source.InvocationCallerId));
                }
            }
        }

        static RuntimeSourceElementKey Caller(ProgramSourceMapEntry invocation,
            Dictionary<string, string> graphByPath)
        {
            if (string.IsNullOrEmpty(invocation.ParentInvocationPath))
                return RuntimeSourceElementKey.Graph(invocation.GraphId);
            if (!graphByPath.TryGetValue(invocation.ParentInvocationPath, out string parentGraphId))
                throw new InvalidOperationException("技能子图调用缺少父图来源。");
            return invocation.InvocationCallerKind switch
            {
                ProgramInvocationCallerKind.Node =>
                    RuntimeSourceElementKey.Node(parentGraphId, invocation.InvocationCallerId),
                ProgramInvocationCallerKind.Edge =>
                    RuntimeSourceElementKey.Edge(parentGraphId, invocation.InvocationCallerId),
                ProgramInvocationCallerKind.TimelineClip or
                ProgramInvocationCallerKind.PresentationMarker or
                ProgramInvocationCallerKind.PresentationTreeClip =>
                    RuntimeSourceElementKey.Node(parentGraphId, TimelineCallerNode(invocation.SourceInvocationPath)),
                _ => RuntimeSourceElementKey.Graph(parentGraphId)
            };
        }

        static string TimelineCallerNode(string path)
        {
            int timeline = path.LastIndexOf("/timeline:", StringComparison.Ordinal);
            int node = timeline < 0 ? -1 : path.LastIndexOf("/node:", timeline, StringComparison.Ordinal);
            int start = node + "/node:".Length;
            if (node < 0 || start >= timeline)
                throw new InvalidOperationException("Timeline 子图调用路径缺少作者节点。");
            return path.Substring(start, timeline - start);
        }

        public static RuntimeSourceElementKey SourceKey(ProgramSourceMapEntry source)
        {
            if (source.TargetKind == ProgramSourceTargetKind.GraphInvocation)
                return RuntimeSourceElementKey.Graph(source.GraphId);
            if (source.TargetKind == ProgramSourceTargetKind.OperationPort &&
                !string.IsNullOrEmpty(source.NodeId) && !string.IsNullOrEmpty(source.PortId))
                return RuntimeSourceElementKey.Port(source.GraphId, source.NodeId, source.PortId);
            if (!string.IsNullOrEmpty(source.EdgeId))
                return RuntimeSourceElementKey.Edge(source.GraphId, source.EdgeId);
            if (!string.IsNullOrEmpty(source.DeclarationId))
                return RuntimeSourceElementKey.Declaration(source.GraphId, source.DeclarationId);
            if (!string.IsNullOrEmpty(source.NodeId))
                return RuntimeSourceElementKey.Node(source.GraphId, source.NodeId);
            if (!string.IsNullOrEmpty(source.ClipId))
                return RuntimeSourceElementKey.Clip(source.TimelineId, source.TrackId, source.ClipId, graphId: source.GraphId);
            if (!string.IsNullOrEmpty(source.TrackId))
                return RuntimeSourceElementKey.Track(source.TimelineId, source.TrackId, source.GraphId);
            return !string.IsNullOrEmpty(source.TimelineId)
                ? RuntimeSourceElementKey.Timeline(source.TimelineId, source.GraphId)
                : RuntimeSourceElementKey.Graph(source.GraphId);
        }

        static RuntimeSourceTargetKind MapTargetKind(ProgramSourceTargetKind kind)
        {
            return kind switch
            {
                ProgramSourceTargetKind.Constant => RuntimeSourceTargetKind.Constant,
                ProgramSourceTargetKind.StateSlot => RuntimeSourceTargetKind.StateSlot,
                ProgramSourceTargetKind.Reference => RuntimeSourceTargetKind.Reference,
                ProgramSourceTargetKind.Producer => RuntimeSourceTargetKind.Producer,
                ProgramSourceTargetKind.CatalogEntry => RuntimeSourceTargetKind.CatalogEntry,
                _ => RuntimeSourceTargetKind.Operation
            };
        }
    }
}
