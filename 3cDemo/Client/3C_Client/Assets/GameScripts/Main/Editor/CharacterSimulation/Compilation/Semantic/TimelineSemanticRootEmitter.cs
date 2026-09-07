using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public sealed class TimelineSemanticRootEmissionRequest
    {
        public TimelineSemanticRootEmissionRequest(
            TimelineSemanticContentRecord content,
            CharacterSimulationProgramBuilder builder,
            TimelineSemanticInvocation invocation,
            OperationHandle rootOperation,
            OperationHandle treeStateScopeOwner,
            string actionContextIdentity,
            Func<TimelineSemanticClipRecord, OperationHandle, TimelineSemanticTreeCompilation> treeCompiler)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            Invocation = invocation;
            if (!invocation.IsValid || invocation.Kind != TimelineSemanticInvocationKind.IndependentRoot)
                throw new ArgumentException("Timeline root invocation is invalid.", nameof(invocation));
            if (!string.Equals(invocation.ContentIdentity, content.ContentUnit.ContentHash, StringComparison.Ordinal))
                throw new ArgumentException("Timeline root invocation content identity does not match the discovered content.", nameof(invocation));
            RootOperation = rootOperation.IsValid
                ? rootOperation
                : throw new ArgumentException("Timeline root operation is required.", nameof(rootOperation));
            TreeStateScopeOwner = treeStateScopeOwner;
            ActionContextIdentity = actionContextIdentity?.Trim() ?? string.Empty;
            TreeCompiler = treeCompiler;
        }

        public TimelineSemanticContentRecord Content { get; }
        public CharacterSimulationProgramBuilder Builder { get; }
        public TimelineSemanticInvocation Invocation { get; }
        public OperationHandle RootOperation { get; }
        public OperationHandle TreeStateScopeOwner { get; }
        public string ActionContextIdentity { get; }
        public Func<TimelineSemanticClipRecord, OperationHandle, TimelineSemanticTreeCompilation> TreeCompiler { get; }
    }

    internal sealed class TimelineSemanticRootEmitter
    {
        readonly TimelineSemanticEmitter m_Emitter;

        public TimelineSemanticRootEmitter(TimelineSemanticEmitter emitter)
        {
            m_Emitter = emitter ?? throw new ArgumentNullException(nameof(emitter));
        }

        public TimelineSemanticRootEmissionResult Emit(TimelineSemanticRootEmissionRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            TimelineData timeline = request.Content.Timeline;
            CharacterSimulationSourceLocation source = new CharacterSimulationSourceLocation(
                typeof(TimelineData).FullName,
                string.Empty,
                string.Empty,
                string.Empty,
                timeline.AuthoringId,
                string.Empty,
                $"{request.Invocation.Route}/root:{request.Invocation.RootIdentity}/entry:{request.Invocation.EntryIdentity}",
                contentHash: request.Content.ContentUnit.ContentHash);
            int actionContext = request.ActionContextIdentity.Length == 0
                ? -1
                : request.Builder.DeclareConstant(source, "ActionContext", request.ActionContextIdentity);
            OperationHandle timelineOperation = request.Builder.DeclareOperation(
                source,
                SimulationOperationCode.Timeline,
                actionContext >= 0 ? new[] { actionContext } : Array.Empty<int>(),
                integer0: (int)request.Invocation.PlaybackMode,
                text0: timeline.AuthoringId);
            TimelineSemanticEmissionResult timelineResult = m_Emitter.Emit(
                new TimelineSemanticEmissionRequest(
                    request.Content,
                    request.Builder,
                    request.Invocation,
                    timelineOperation,
                    request.TreeStateScopeOwner,
                    request.ActionContextIdentity,
                    request.TreeCompiler));

            request.Builder.DeclareControlFlow(
                request.Invocation.ReferenceIdentity($"root-entry:{request.Invocation.EntryIdentity}"),
                request.RootOperation,
                timelineOperation,
                "Entry",
                "Entry",
                ProgramControlFlowKind.Child,
                0,
                0,
                ProgramAbortPolicy.None,
                false,
                OperationHandle.Invalid,
                source);
            return new TimelineSemanticRootEmissionResult(request.RootOperation, timelineResult);
        }
    }

    public sealed class TimelineSemanticRootEmissionResult
    {
        internal TimelineSemanticRootEmissionResult(
            OperationHandle rootOperation,
            TimelineSemanticEmissionResult timeline)
        {
            RootOperation = rootOperation;
            Timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
        }

        public OperationHandle RootOperation { get; }
        public TimelineSemanticEmissionResult Timeline { get; }
        public OperationHandle TimelineOperation => Timeline.TimelineOperation;
        public IReadOnlyList<OperationHandle> ClipOperations => Timeline.ClipOperations;
        public bool IsValid => RootOperation.IsValid && Timeline.IsValid;
    }
}
