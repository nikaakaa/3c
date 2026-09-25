using System.Collections.Generic;
using BTSMTL.Diagnostics;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    public static class AbilityDebugSourceMapFiller
    {
        public static void Fill(DebugSourceMap sourceMap, int targetIndexOffset, IReadOnlyList<ProgramSourceMapEntry> sources)
        {
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
                        key, source.InvocationCallerClipId, source.InvocationCallerId));
                }
            }
        }

        internal static RuntimeSourceElementKey SourceKey(ProgramSourceMapEntry source)
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

