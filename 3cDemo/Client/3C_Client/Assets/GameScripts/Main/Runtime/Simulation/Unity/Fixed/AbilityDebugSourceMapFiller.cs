using BTSMTL.Diagnostics;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Unity.Fixed
{
    internal static class AbilityDebugSourceMapFiller
    {
        public static void Fill(DebugSourceMap sourceMap, int targetIndexOffset, IReadOnlyList<ProgramSourceMapEntry> sources)
        {
            foreach (ProgramSourceMapEntry source in sources)
            {
                if (source.TargetKind == ProgramSourceTargetKind.OptimizedAway)
                    continue;
                ProgramSourceTargetKind targetKind =
                    source.TargetKind == ProgramSourceTargetKind.OperationPort
                        ? ProgramSourceTargetKind.Operation
                        : source.TargetKind;
                RuntimeSourceElementKind kind =
                    source.TargetKind == ProgramSourceTargetKind.GraphInvocation
                        ? RuntimeSourceElementKind.Graph
                        : !string.IsNullOrEmpty(source.ClipId)
                            ? RuntimeSourceElementKind.Clip
                            : !string.IsNullOrEmpty(source.TrackId)
                                ? RuntimeSourceElementKind.Track
                                : !string.IsNullOrEmpty(source.TimelineId)
                                    ? RuntimeSourceElementKind.Timeline
                                    : RuntimeSourceElementKind.Node;
                string elementId = kind == RuntimeSourceElementKind.Graph ? source.GraphId : source.NodeId;
                var key = new RuntimeSourceElementKey(kind, source.GraphId, elementId, source.TimelineId, source.TrackId, source.ClipId);
                var target = new RuntimeSourceTarget(MapTargetKind(targetKind), targetIndexOffset + source.TargetIndex);
                sourceMap.Add(key, default, source.DisplayPath, source.ContentHash, target);
                if (source.TargetKind == ProgramSourceTargetKind.GraphInvocation)
                {
                    sourceMap.AddGraphInvocation(new RuntimeGraphInvocation(
                        source.GraphInvocationPath, source.GraphId, source.ParentInvocationPath,
                        key, source.InvocationCallerClipId, source.InvocationCallerId));
                }
            }
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
                ProgramSourceTargetKind.GraphInvocation => RuntimeSourceTargetKind.Reference,
                _ => RuntimeSourceTargetKind.Operation
            };
        }
    }
}