using System;
using FlowCanvas;
using ThirdPersonCharacter.Control.Authoring;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public interface IBtsmtlSkillFlowMutationDispatcher
    {
        T Execute<T>(FlowGraph graph, string title, Func<T> mutation);
        void Apply(FlowGraph graph, string title, Action mutation, bool recordUndo);
        void Apply(FlowGraph graph, string title, Action mutation, bool recordUndo, bool validateClosure);
        bool CanConnect(FlowGraph graph, Port source, Port target, out string reason);
    }

    public sealed class BtsmtlSkillFlowMutationDispatcher : IBtsmtlSkillFlowMutationDispatcher
    {
        public T Execute<T>(FlowGraph graph, string title, Func<T> mutation) =>
            BtsmtlSkillFlowEditorMutation.Execute(graph, title, mutation);

        public void Apply(FlowGraph graph, string title, Action mutation, bool recordUndo) =>
            BtsmtlSkillFlowEditorMutation.Apply(graph, title, mutation, recordUndo);

        public void Apply(FlowGraph graph, string title, Action mutation, bool recordUndo, bool validateClosure) =>
            BtsmtlSkillFlowEditorMutation.Apply(
                graph,
                title,
                mutation,
                recordUndo,
                Array.Empty<UnityEngine.Object>(),
                validateClosure);

        public bool CanConnect(FlowGraph graph, Port source, Port target, out string reason) =>
            BtsmtlSkillFlowEditorMutation.CanConnect(graph, source, target, out reason);
    }
}
