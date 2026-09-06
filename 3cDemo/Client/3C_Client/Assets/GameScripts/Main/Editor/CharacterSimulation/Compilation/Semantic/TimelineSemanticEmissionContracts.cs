using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BTSMTL.Timeline;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public enum TimelineSemanticInvocationKind : byte
    {
        CharacterGraph = 1,
        IndependentRoot = 2
    }

    public readonly struct TimelineSemanticInvocation
    {
        public TimelineSemanticInvocation(
            TimelineSemanticInvocationKind kind,
            string route,
            string graphId,
            string nodeId,
            TimelinePlaybackMode playbackMode,
            string rootIdentity,
            string entryIdentity,
            string contentIdentity)
        {
            if (!Enum.IsDefined(typeof(TimelineSemanticInvocationKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            Route = RequireRoute(route);
            GraphId = graphId?.Trim() ?? string.Empty;
            NodeId = nodeId?.Trim() ?? string.Empty;
            RootIdentity = rootIdentity?.Trim() ?? string.Empty;
            EntryIdentity = entryIdentity?.Trim() ?? string.Empty;
            ContentIdentity = contentIdentity?.Trim() ?? string.Empty;
            if (kind == TimelineSemanticInvocationKind.CharacterGraph &&
                (GraphId.Length == 0 || NodeId.Length == 0 ||
                 RootIdentity.Length != 0 || EntryIdentity.Length != 0 || ContentIdentity.Length != 0))
            {
                throw new ArgumentException("Character Timeline invocation requires only Graph and Node identities.");
            }
            if (kind == TimelineSemanticInvocationKind.IndependentRoot &&
                (GraphId.Length != 0 || NodeId.Length != 0 ||
                 RootIdentity.Length == 0 || EntryIdentity.Length == 0 || ContentIdentity.Length == 0))
            {
                throw new ArgumentException("Independent Timeline invocation requires root, entry and content identities and cannot carry Graph or Node identities.");
            }
            if (!Enum.IsDefined(typeof(TimelinePlaybackMode), playbackMode))
                throw new ArgumentOutOfRangeException(nameof(playbackMode));
            Kind = kind;
            PlaybackMode = playbackMode;
        }

        public static TimelineSemanticInvocation ForCharacterGraph(
            string route,
            string graphId,
            string nodeId,
            TimelinePlaybackMode playbackMode)
        {
            return new TimelineSemanticInvocation(
                TimelineSemanticInvocationKind.CharacterGraph,
                route,
                graphId,
                nodeId,
                playbackMode,
                string.Empty,
                string.Empty,
                string.Empty);
        }

        public static TimelineSemanticInvocation ForIndependentRoot(
            string route,
            TimelinePlaybackMode playbackMode,
            string rootIdentity,
            string entryIdentity,
            string contentIdentity)
        {
            return new TimelineSemanticInvocation(
                TimelineSemanticInvocationKind.IndependentRoot,
                route,
                string.Empty,
                string.Empty,
                playbackMode,
                rootIdentity,
                entryIdentity,
                contentIdentity);
        }

        public TimelineSemanticInvocationKind Kind { get; }
        public string Route { get; }
        public string GraphId { get; }
        public string NodeId { get; }
        public TimelinePlaybackMode PlaybackMode { get; }
        public string RootIdentity { get; }
        public string EntryIdentity { get; }
        public string ContentIdentity { get; }
        public string Identity => Kind == TimelineSemanticInvocationKind.IndependentRoot
            ? $"{Kind}:{RootIdentity}:{EntryIdentity}:{Route}"
            : $"{Kind}:{Route}";
        public bool IsValid => Enum.IsDefined(typeof(TimelineSemanticInvocationKind), Kind) &&
            Route?.Length > 0 &&
            Enum.IsDefined(typeof(TimelinePlaybackMode), PlaybackMode) &&
            (Kind == TimelineSemanticInvocationKind.CharacterGraph
                ? GraphId?.Length > 0 && NodeId?.Length > 0
                : Kind == TimelineSemanticInvocationKind.IndependentRoot &&
                  GraphId?.Length == 0 && NodeId?.Length == 0 &&
                  RootIdentity?.Length > 0 && EntryIdentity?.Length > 0 && ContentIdentity?.Length > 0);

        public string ReferenceIdentity(string identity)
        {
            return string.IsNullOrWhiteSpace(identity)
                ? throw new ArgumentException("Timeline reference identity is required.", nameof(identity))
                : $"{Identity}/{identity.Trim()}";
        }

        static string RequireRoute(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Timeline invocation route is required.", nameof(value))
                : value.Trim();
        }
    }

    public sealed class TimelineSemanticEmissionRequest
    {
        public TimelineSemanticEmissionRequest(
            TimelineSemanticContentRecord content,
            CharacterSimulationProgramBuilder builder,
            TimelineSemanticInvocation invocation,
            OperationHandle timelineOperation,
            OperationHandle treeStateScopeOwner,
            string actionContextIdentity,
            Func<TimelineSemanticClipRecord, OperationHandle, TimelineSemanticTreeCompilation> treeCompiler)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            Invocation = invocation;
            if (!invocation.IsValid)
                throw new ArgumentException("Timeline invocation is invalid.", nameof(invocation));
            if (invocation.Kind == TimelineSemanticInvocationKind.IndependentRoot &&
                !string.Equals(invocation.ContentIdentity, content.ContentUnit.ContentHash, StringComparison.Ordinal))
            {
                throw new ArgumentException("Independent Timeline invocation content identity does not match the discovered content.", nameof(invocation));
            }
            TimelineOperation = timelineOperation.IsValid
                ? timelineOperation
                : throw new ArgumentException("Timeline operation is required.", nameof(timelineOperation));
            TreeStateScopeOwner = treeStateScopeOwner;
            ActionContextIdentity = actionContextIdentity?.Trim() ?? string.Empty;
            TreeCompiler = treeCompiler;
        }

        public TimelineSemanticContentRecord Content { get; }
        public CharacterSimulationProgramBuilder Builder { get; }
        public TimelineSemanticInvocation Invocation { get; }
        public OperationHandle TimelineOperation { get; }
        public OperationHandle TreeStateScopeOwner { get; }
        public string ActionContextIdentity { get; }
        public Func<TimelineSemanticClipRecord, OperationHandle, TimelineSemanticTreeCompilation> TreeCompiler { get; }
    }

    public sealed class TimelineSemanticEmissionResult
    {
        internal TimelineSemanticEmissionResult(
            OperationHandle timelineOperation,
            IEnumerable<OperationHandle> clipOperations,
            CharacterSimulationCompileReport report)
        {
            TimelineOperation = timelineOperation;
            ClipOperations = new ReadOnlyCollection<OperationHandle>(
                new List<OperationHandle>(clipOperations ?? Array.Empty<OperationHandle>()));
            Report = report ?? throw new ArgumentNullException(nameof(report));
        }

        public OperationHandle TimelineOperation { get; }
        public IReadOnlyList<OperationHandle> ClipOperations { get; }
        public CharacterSimulationCompileReport Report { get; }
        public bool IsValid => TimelineOperation.IsValid && Report.IsValid;
    }
}
